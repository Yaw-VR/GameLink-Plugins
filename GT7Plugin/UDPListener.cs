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

        private Task receiveTask;
        private CancellationTokenSource cancellationTokenSource;

        public event PacketReceived OnPacketReceived;

        Timer hbTimer;
        public UDPListener(int port = 33740)
        {
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

            receiveTask = Receive(cancellationTokenSource.Token);
        }
        private async Task Receive(CancellationToken cancelToken)
        {
            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    UdpReceiveResult res = await udpClient.ReceiveAsync(cancelToken);

                    OnPacketReceived.Invoke(this,res.Buffer);
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
            udpClient.Send(Encoding.UTF8.GetBytes("A"), remoteEndPoint);
        }
    }
}
