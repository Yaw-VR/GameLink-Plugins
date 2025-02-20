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
using YawGLAPI;
namespace YawVR_Game_Engine.Plugin
{

    [Export(typeof(Game))]
    [ExportMetadata("Name", "1976 Back to Midway")]
    [ExportMetadata("Version", "1.0")]
    class _1976BTM : Game {

        private static int BTM_TELEMETRY_PORT = 4123;
        private bool stopThread;

        private UdpClient udpClient;

        private Thread readThread;
        private IPEndPoint remotePoint;
        public int STEAM_ID => 1118070;
        public string PROCESS_NAME => string.Empty;
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "YawVR";


        public string Description => String.Empty;
        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        private IProfileManager controller;

        public LedEffect DefaultLED() {
            return new LedEffect(

              EFFECT_TYPE.FLOW_LEFTRIGHT,
              1,
              new YawColor[] {
                new YawColor(66, 135, 245),
                new YawColor(80,80,80),
                new YawColor(128, 3, 117),
                new YawColor(110, 201, 12),
                },
               0.7f);
        }
        public List<Profile_Component> DefaultProfile() {
            return new List<Profile_Component>() {
               // new Profile_Component(0,0, 1,false,false,-1,1f),
                new Profile_Component(0,1, 0.4f,0.4f,0f,false,false,-1,0.6f),
                new Profile_Component(1,2, 0.4f,0.4f,0f,false,false,-1,0.6f)
            };
        }
        public void Exit() {
            udpClient.Close();
            udpClient = null;
            stopThread = true;
        }

        public string[] GetInputData() {
            return new string[] {
                "Pitch","Roll"
            };
        }

       
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
        }
        public void Init() {

           
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }


        private void ReadFunction() {
            udpClient = new UdpClient(BTM_TELEMETRY_PORT);
            Console.WriteLine("1976 udp listening started");
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

                       // float yaw = float.Parse(rawData[2], CultureInfo.InvariantCulture);
                        float pitch = float.Parse(rawData[1], CultureInfo.InvariantCulture) * -10;
                        float roll = float.Parse(rawData[0], CultureInfo.InvariantCulture) * 10;
                      //  float speed = float.Parse(rawData[8], CultureInfo.InvariantCulture);

                       // controller.SetInput(0, yaw);
                        controller.SetInput(0, pitch);
                        controller.SetInput(1, roll);
                       // controller.SetInput(3, speed);
                    }
                }

            }
            catch (ThreadAbortException) { }
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
            var fullResourceName = $"_1976BackToMidway.Resources.{resourceName}";
            return assembly.GetManifestResourceStream(fullResourceName);
            
        }

        public Type GetConfigBody()
        {
            return null;
        }
    }
}
