using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using WhitewaterPlugin.Properties;
using YawGLAPI;

namespace WhitewaterPlugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "WhitewaterVR")]
    [ExportMetadata("Version", "1.0")]
    public class WhitewaterPlugin : Game
    {
        public int STEAM_ID => 2360340;

        public string PROCESS_NAME => "WhitewaterVR-Win64-Shipping";

        public bool PATCH_AVAILABLE => false;

        public string AUTHOR => "YawVR";

        public Stream Logo => GetStream("logo.png");

        public Stream SmallLogo => GetStream("recent.png");

        public Stream Background => GetStream("wide.png");

        public string Description => Resources.Description;


        private Thread receiveThread;
        private UdpClient client;
        private bool reading = false;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;

        public void Init()
        {

            client = new UdpClient(33001);
            client.Client.ReceiveTimeout = 500;
            reading = true;
            receiveThread = new Thread(new ThreadStart(ReadThread));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        public void Exit()
        {
            client.Close();
            client = null;
            reading = false;
        }

        private void ReadThread()
        {
            FieldInfo[] fields = typeof(TelemetryPacket).GetFields();
            IPEndPoint from = new IPEndPoint(IPAddress.Any,33001);
            TelemetryPacket packet = new TelemetryPacket();
            while (reading)
            {
                try
                {
                    byte[] recv = client.Receive(ref from);


                    IntPtr unmanagedPointer =
                    Marshal.AllocHGlobal(recv.Length);
                    Marshal.Copy(recv, 160, unmanagedPointer, recv.Length - 160);
                    // Call unmanaged code
                    Marshal.FreeHGlobal(unmanagedPointer);
                    Marshal.PtrToStructure(unmanagedPointer, packet);

                    for (int i = 0; i < fields.Length; i++)
                    {
                        controller.SetInput(i, Convert.ToSingle(fields[i].GetValue(packet)));
                    }

                }
                catch (SocketException) { } // Timeout
            }
        }

        public LedEffect DefaultLED()
        {
            return new LedEffect();
        }

        public List<Profile_Component> DefaultProfile()
        {
            return new List<Profile_Component>();
        }

       

        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return new Dictionary<string, ParameterInfo[]>();
        }

        public string[] GetInputData()
        {
            FieldInfo[] fields = typeof(TelemetryPacket).GetFields();

            return fields.Select(f => f.Name).ToArray();
        }

     

        public void PatchGame()
        {
            return;
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        Stream GetStream(string resourceName)
        {
            var assembly = GetType().Assembly;
            var rr = assembly.GetManifestResourceNames();
            string fullResourceName = $"{assembly.GetName().Name}.Resources.{resourceName}";
            return assembly.GetManifestResourceStream(fullResourceName);
        }

        public Type GetConfigBody()
        {
            return null;
        }
    }
}
