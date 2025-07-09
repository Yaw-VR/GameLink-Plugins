using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

using Timer = System.Timers.Timer;
namespace GT7Plugin
{
    public class UDPListener
    {
        public delegate void PacketReceived(object sender, byte[] buffer);
        private int heartBeatPort = 33739;

        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;

        
        private CancellationTokenSource cancellationTokenSource;

        public event PacketReceived OnPacketReceived;

        private byte[] _heartbeatBytes;

        Timer hbTimer;

        private SimInterfacePacketType _packetType;

        private SimulatorInterfaceCryptorGT7 _cryptor;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="packetType"></param>
        public UDPListener(SimInterfacePacketType packetType, int port = 33740)
        {
            _packetType = packetType;

            _heartbeatBytes = (packetType switch
            {
                SimInterfacePacketType.PacketType1 => "A"u8,
                SimInterfacePacketType.PacketType2 => "B"u8,
                SimInterfacePacketType.PacketType3 => "~"u8,
                _ => "A"u8, // We should default to "~".
            }).ToArray();

            _cryptor = new (packetType);

            cancellationTokenSource = new CancellationTokenSource();
            udpClient = new UdpClient(port);

            if (port == 33740) {
                // only send heartbeats if we are listening on the default port,
                // otherwise the data is coming from localhost which is NOT the playstation.
                remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, heartBeatPort);

                hbTimer = new Timer(TimeSpan.FromSeconds(1));
                hbTimer.Elapsed += SendHeartbeat;
                hbTimer.Enabled = true;
            }

            _ = Receive(cancellationTokenSource.Token);
        }

        private async Task Receive(CancellationToken cancelToken)
        {
            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    UdpReceiveResult result = await udpClient.ReceiveAsync(cancelToken);

                    if (result.Buffer.Length == GetExpectedPacketSize())
                    {
                        _cryptor.Decrypt(result.Buffer);
                        OnPacketReceived.Invoke(this, result.Buffer);
                    }
                    else
                    {
                        //throw new InvalidDataException($"Expected packet size to be 0x{GetExpectedPacketSize():X} bytes. Was {result.Buffer.Length:X4} bytes.");
                    }
                }
            } 
            catch(OperationCanceledException)
            {
            }
        }

        public void Stop()
        {
            hbTimer.Stop();
            cancellationTokenSource.Cancel();

            udpClient.Dispose();
            udpClient = null;
        }

        private void SendHeartbeat(object? sender, ElapsedEventArgs e)
        {
            udpClient.Send(_heartbeatBytes, remoteEndPoint);
        }

        /// <summary>
        /// TODO: Game might send a packet of 0x94 if not using 'A' type heartbeat?
        /// Might need checking. Might also be exclusive to GT7 >= 1.42
        /// </summary>
        /// <returns></returns>
        private uint GetExpectedPacketSize()
        {
            return _packetType switch
            {
                SimInterfacePacketType.PacketType1 => 0x128,
                SimInterfacePacketType.PacketType2 => 0x13C,
                SimInterfacePacketType.PacketType3 => 0x158,
                _ => 0x128,
            };
        }
    }
}
