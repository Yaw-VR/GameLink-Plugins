using HelicopterSimulator2021Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using YawGLAPI;
namespace HelicopterSimulator
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Helicopter Simulator 2021 VR")]
    [ExportMetadata("Version", "1.0")]
    public class HelicopterSimulator2021Plugin : Game
    {
        public int STEAM_ID => 1573730;

        public string PROCESS_NAME => "HelicopterSimulatorVR";

        public bool PATCH_AVAILABLE => false;

        public string AUTHOR => "YawVR";

        public Stream Background => GetStream("wide.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Logo => GetStream("logo.png");
        public string Description => Resources.description;

        private const int Port = 4123;
        private UdpClient udpClient;
        private Thread thread;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private IPEndPoint endp = new IPEndPoint(IPAddress.Any,Port);
        private bool running = false;

        public LedEffect DefaultLED()
        {
            return dispatcher.JsonToLED(Resources.defProfile);
        }

        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit()
        {
            running = false;
            udpClient.Close();
        }

   
        public string[] GetInputData()
        {
            return new string[]
            {
                "Yaw","Pitch","Roll",
            };
        }

        public void Init()
        {
            udpClient = new UdpClient(Port);
            udpClient.Client.ReceiveTimeout = 2000;
            running = true;
            thread = new Thread(Run);
            thread.Start();
        }
        private void Run()
        {
            try
            {
                while (running)
                {
                    try
                    {
                        string[] data = Encoding.ASCII.GetString(udpClient.Receive(ref endp)).Split(',');

                        controller.SetInput(0, float.Parse(data[2]) / 180f);
                        controller.SetInput(1, float.Parse(data[1]) / 180f);
                        controller.SetInput(2, float.Parse(data[0]) / 180f);
                    } catch(SocketException) { } //Timeout
                }
            } catch(ThreadAbortException)
            {

            }
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

        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return null;
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
