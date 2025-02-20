using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using TouringKartsPlugin.Properties;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{

    [Export(typeof(Game))]
    [ExportMetadata("Name","Touring Karts")]
    [ExportMetadata("Version", "1.0")]
    class TouringKartsPlugin : Game {

        private static int TOURING_TELEMETRY_PORT = 4123;
        private bool stopThread;

        private UdpClient udpClient;

        private Thread readThread;
        private IPEndPoint remotePoint;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        public string PROCESS_NAME => "Touring Karts";
        public int STEAM_ID => 1088950;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => false;

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        public LedEffect DefaultLED() {
            return dispatcher.JsonToLED(Resources.defProfile);
        }
        public List<Profile_Component> DefaultProfile() {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }
        public void Exit() {
            udpClient.Close();
            udpClient = null;
            stopThread = true;
        }

        public string[] GetInputData() {
            return new string[] {
                "Yaw","Pitch","Roll","Speed"
            };
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

     
        public void Init() {

            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }


        private void ReadFunction() {
            udpClient = new UdpClient(TOURING_TELEMETRY_PORT);
            Console.WriteLine("touring karts udp listening started");
            try {
                while (!stopThread) {
                    {

                        byte[] data = udpClient.Receive(ref remotePoint);

                        string receive = Encoding.ASCII.GetString(data);

                        if (receive == ",,,,,,,,") {
                            continue;
                        }
                     
                        //Roll,Pitch,Yaw,empty,empty,empty,empty,empty,speed
                        string[] rawData = receive.Split(',');

                        float yaw = float.Parse(rawData[2], CultureInfo.InvariantCulture);
                        float pitch = float.Parse(rawData[1], CultureInfo.InvariantCulture) * -10;
                        float roll = float.Parse(rawData[0], CultureInfo.InvariantCulture) * 10;
                        float speed = float.Parse(rawData[8], CultureInfo.InvariantCulture);

                        controller.SetInput(0, yaw);
                        controller.SetInput(1, pitch);
                        controller.SetInput(2, roll);
                        controller.SetInput(3, speed);
                    }
                }

            } catch(ThreadAbortException) { }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        public void PatchGame()
        {
            return;
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
