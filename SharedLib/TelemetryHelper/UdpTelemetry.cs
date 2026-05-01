using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLib.TelemetryHelper
{
    internal record UdpTelemetryConfig
    {
        public IPEndPoint SendAddress { get; set; }

        public IPEndPoint ReceiveAddress { get; set; }

        public int ReceiveTimeout { get; set; } = 0;

        public UdpTelemetryConfig()
        {
             
        }

        /// <summary>
        /// Configure the UDP plugin with send and receive addresses and ports.
        /// </summary>
        /// <param name="sendAddress">ipaddress:port</param>
        /// <param name="receiveAddress">ipaddress:port</param>      
        public UdpTelemetryConfig(string sendAddress = null, string receiveAddress = null)
        {
            SendAddress = ParseAddressAndPort(sendAddress);
            ReceiveAddress = ParseAddressAndPort(receiveAddress);
        }

        /// <summary>
        /// Configure the UDP plugin with send and receive addresses and ports.
        /// </summary>
        /// <param name="sendAddress">send address</param>
        /// <param name="receiveAddress">receive address</param>      
        public UdpTelemetryConfig(IPEndPoint sendAddress = null, IPEndPoint receiveAddress = null)
        {
            SendAddress = sendAddress;
            ReceiveAddress = receiveAddress;
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

    internal class UdpTelemetry<TData> : TelemetryBase<TData, UdpTelemetryConfig>
       // where TData : struct
    {

        public override bool IsConnected =>
            udpClient != null &&
            udpClient.Client != null &&
            udpClient.Client.Connected &&
            udpClient.Client.IsBound &&
            !(udpClient.Client.SafeHandle?.IsInvalid ?? true) &&
            !(udpClient.Client.SafeHandle?.IsClosed ?? true);

        private static UdpClient udpClient;

        public event Action<UdpReceiveResult, TData> OnReceiveAsync;

        public UdpTelemetry(UdpTelemetryConfig config, IByteConverter<TData> converter) : base(config, converter)
        {
        }

        protected override void Configure(UdpTelemetryConfig config)
        {
            if (config.ReceiveAddress != null)
            {
                Log($"Create UdpClient: Receiving @ {config.ReceiveAddress.Address}: {config.ReceiveAddress.Port} with timeout of {Config.ReceiveTimeout} ms");
                udpClient = new UdpClient(config.ReceiveAddress);                
            }
            else
            {
                Log($"Create UdpClient");
                udpClient = new UdpClient();
            }

            if (config.SendAddress != null)
            {
                Log($"Create Send Adress {config.SendAddress.Address}: {config.SendAddress.Port} with timeout of {Config.ReceiveTimeout} ms");
                //udpClient.Connect(config.SendAddress);
            }

            udpClient.Client.ReceiveTimeout = Config.ReceiveTimeout;
        }

        public override TData Receive()
        {
            IPEndPoint remoteEp = null;
            var bytes = udpClient.Receive(ref remoteEp);

            var data = Converter.FromBytes(bytes);

            OnReceiveAsync?.Invoke(new UdpReceiveResult(bytes, remoteEp), data);

            return data;

        }

        public override int Send(TData data)
        {
            var bytes = Converter.ToBytes(data);
            //return udpClient.Send(bytes, bytes.Length);
            if (Config.SendAddress != null)
            {
                return udpClient.Send(bytes, Config.SendAddress);
            }

            return 0;
        }

        public override void Dispose()
        {
            udpClient.Close();
        }


        public override async Task<TData> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(cancellationToken);

                var data = Converter.FromBytes(result.Buffer);
                OnReceiveAsync?.Invoke(result, data);
                return data;
            }
            catch (OperationCanceledException)
            {
                Log("ReceiveAsync operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                Log($"An error occurred during ReceiveAsync: {ex.Message}");
                //throw;
            }
            finally
            {
               // udpClient?.Close();
            }
            return default(TData);
        }        

    }
}
