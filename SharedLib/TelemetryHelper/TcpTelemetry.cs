using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLib.TelemetryHelper
{
    internal class TcpTelemetryConfig
    {
        /// <summary>
        /// Set to true if you are the caller otherwise set to false if you are the receiver.
        /// </summary>
        public bool IsCaller { get; set; }

        public IPEndPoint IpAddress { get; set; }

        public int ReceiveTimeout { get; set; } = 0;

        public TcpTelemetryConfig()
        {

        }

        /// <summary>
        /// Configure the TCP plugin with send and receive addresses and ports.
        /// </summary>
        /// <param name="sendAddress">ipaddress:port</param>
        /// <param name="receiveAddress">ipaddress:port</param>      
        public TcpTelemetryConfig(string sendAddress = null)
        {
            IpAddress = ParseAddressAndPort(sendAddress);
        }

        /// <summary>
        /// Configure the TCP plugin with send and receive addresses and ports.
        /// </summary>
        /// <param name="sendAddress">send address</param>
        /// <param name="receiveAddress">receive address</param>      
        public TcpTelemetryConfig(IPEndPoint sendAddress = null)
        {
            IpAddress = sendAddress;
        }

        private IPEndPoint ParseAddressAndPort(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            var parts = address.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid address format. Expected format: ipaddress:port");
            var ip = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);
            return new IPEndPoint(ip, port);
        }
        
    }

    internal class TcpTelemetry<TData> : TelemetryBase<TData, TcpTelemetryConfig> //where TData : struct
    {
        

        private static readonly System.Collections.Concurrent.ConcurrentBag<TcpClient> tcpClients = new();

        public event Action<object,TData> OnReceiveAsync;

        public override bool IsConnected => tcpClients.Any(c => c?.Connected ?? false);


        CancellationTokenSource cts;

        
        public TcpTelemetry(TcpTelemetryConfig config, IByteConverter<TData> converter) : base(config, converter)
        {
        }

        protected override void Configure(TcpTelemetryConfig config)
        {
            
            
            cts = new CancellationTokenSource();


            if (config.IpAddress != null)
            {
                Log($"Create TcpClient");
               
                Log($"Create Send Adress {config.IpAddress.Address}: {config.IpAddress.Port} with timeout of {Config.ReceiveTimeout} ms");
               
                //_ = Task.Factory.StartNew(async () =>
                new Thread(async () =>                
                {
                    while (!cts.IsCancellationRequested)
                    {
                        if (config.IsCaller)
                        {
                            tcpClients.Add(await WaitForConnectAsync(cts.Token));
                        }
                        else
                        {
                            tcpClients.Add(await WaitForCallAsync(cts.Token));
                        }
                    }
                })//, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                    .Start();
                
                
            }
            
        }

        public async Task<TcpClient> WaitForConnectAsync(CancellationToken cancellationToken = default)
        {
            var tcpClient = new TcpClient(Config.IpAddress);
            await tcpClient.ConnectAsync(Config.IpAddress);

            return tcpClient;
        }

        
        public async Task<TcpClient> WaitForCallAsync(CancellationToken cancellationToken = default)
        {
            TcpClient client = null;
            var tcpListener = new TcpListener(Config.IpAddress);
            try 
            {
                

                tcpListener.Start();

                Log($"TCP Listener started on {tcpListener.LocalEndpoint}");

                client = await tcpListener.AcceptTcpClientAsync(cancellationToken);

                Log($"TCP Client connected from {client.Client.RemoteEndPoint}");

                return client;

            }
            catch(OperationCanceledException ocx)
            {
                Log("TCP Listener operation was canceled."); 
            }
            catch(SocketException ex)
            {
                Log($"Error starting TCP Listener: {ex.Message}");
            }
            finally
            {
                try
                {
                    tcpListener.Stop();
                }
                catch(Exception ex)
                {
                    Log($"Error stopping TCP Listener: {ex.Message}");
                }
            }

            return null;
        }

        private bool IsStruct<T>()
        {
            return typeof(T).IsValueType && !typeof(T).IsPrimitive && !typeof(T).IsEnum;
        }

        

        public override TData Receive()
        {
            TData retval = default;

            if (!IsConnected)
            {
                return default;
            }

            //IPEndPoint remoteEp = null;

            int size = GetBufferSizeOrDefault<TData>(1024);
            
            var buffer = new byte[size];


            RemoveDisconnectedClients();

            foreach (var tcpClient in tcpClients)
            {
                //var tcpClient = tcpClients.FirstOrDefault(c => c?.Client?.Connected ?? false);
                
                tcpClient.Client.Receive(buffer, SocketFlags.None);

                var data = Converter.FromBytes(buffer);

                OnReceiveAsync?.Invoke(tcpClient, data);

                retval = data;
                //break;
            }

            return retval;
        }

        private int RemoveDisconnectedClients()
        {
            List<TcpClient> toDelete = tcpClients.Where(c => !(c?.Client.Connected ?? false)).ToList();
            
            foreach (var tcpClient in toDelete)
            {
                try
                {
                    if (tcpClients.TryTake(out var removed))
                        removed?.Close();
                }
                catch (Exception ex)
                {
                    Log($"Error removing TcpClient: {ex.Message}");
                }
            }

            return toDelete.Count;
        }

        public override int Send(TData data)
        {
            int retval = 0;
            try
            {
                if (IsConnected)
                {
                    var bytes = Converter.ToBytes(data);

                    foreach (var tcpClient in tcpClients.Where(c => c?.Connected ?? false))
                    {
                       retval += tcpClient.Client.Send(bytes);
                    }         
                }
            }
            catch (SocketException)
            {

            }
            return retval;
        }

        public override void Dispose()
        {
            foreach (var tcpClient in tcpClients)
            {
                try
                {
                    tcpClient.Close();
                }
                catch (Exception ex)
                {
                    Log($"Error disposing TcpClient: {ex.Message}");
                }                
            }

            tcpClients.Clear();
            cts?.Cancel();
            cts?.Dispose();
        }

        public override async Task<int> SendAsync(TData data, CancellationToken cancellationToken = default)
        {
            int retval = 0;

            if (!IsConnected)
                return retval;
            
            foreach(var tcpClient in tcpClients.Where(c => c?.Connected ?? false))
            {
                var segment = new ArraySegment<byte>(Converter.ToBytes(data));
                retval += await tcpClient.Client.SendAsync(segment, SocketFlags.None).WithCancellation(cancellationToken);
                
            }
            

            return retval;
        }


        public override async Task<TData> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            int size = GetBufferSizeOrDefault<TData>(1024);

            byte[] segment = new byte[size];
            TData retval = default;

            foreach (var tcpClient in tcpClients.Where(c => c?.Connected ?? false))
            {
                try
                {
                    var result = await tcpClient.Client.ReceiveAsync(segment, SocketFlags.None,cancellationToken);
                    if (result > 0)
                    {
                        //Span<byte> trimmed = segment.Array[0..result];
                        var trimmed = new List<byte>(segment.Take(result));
                        retval = Converter.FromBytes(trimmed.ToArray());


                        if (retval != null)
                            OnReceiveAsync?.Invoke(tcpClient, retval);
                    }
                    break;
                }
                catch (OperationCanceledException)
                {
                    Log("ReceiveAsync operation was canceled.");
                    //throw;
                }
                catch (Exception ex)
                {
                    Log($"An error occurred during ReceiveAsync: {ex.Message}");
                    //throw;
                }
            }

            return retval;

        }

        private int GetBufferSizeOrDefault<T>(int defaultSize)
        {
            if (IsValueType<T>())
                return Marshal.SizeOf<T>();
            else
                return defaultSize;
        }
    }
}
