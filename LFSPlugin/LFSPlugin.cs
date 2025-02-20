using LFSPlugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using YawGLAPI;
namespace LFSPlugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Live For Speed")]
    [ExportMetadata("Version", "1.0")]
    class LFSPlugin : Game {

        private UdpClient udpClient;

        private Thread readThread;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        private volatile bool running = false;
        public string PROCESS_NAME => "LFS";
        public int STEAM_ID => 0;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => false;

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        public LedEffect DefaultLED() {
            return new LedEffect(
              
              EFFECT_TYPE.KNIGHT_RIDER,
              3,
              new YawColor[] {
                  new YawColor(203, 230, 131),
                   new YawColor(20,20,20),
                 new YawColor(235, 52, 137),
                 new YawColor(235, 165, 52),
                 },
               5f);
        }

        public List<Profile_Component> DefaultProfile() {
            return new List<Profile_Component>() {
              

                new Profile_Component(0,0, 1,1,0f,false,false,-1,1f), //YAW
                new Profile_Component(3,1, 1,1,0f,false,true,-1,1f), //PITCH_G
                new Profile_Component(4,2, 1,1,0f,false,false,-1,1f), //ROLL_G

                new Profile_Component(1,1, 1,1,0f,false,true,-1,1f), //PITCH
                 new Profile_Component(2,2, 1,1,0f,false,true,-1,1f), //ROLL

            };
        }

        public void Exit() {
            //readThread.Abort();
            running = false;
            udpClient.Close();
            udpClient = null;
        }

        public string[] GetInputData() {
            return new string[] {
                "Yaw",
                "Pitch",
                "Roll",
                "Force_Pitch",
                "Force_Roll",
               };
        }
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
      
        public void Init() {

            var pConfig = dispatcher.GetConfigObject<Config>();
            udpClient = new UdpClient(pConfig.Port);

            udpClient.Client.ReceiveTimeout = 5000;
            running = true;
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }

        private void ReadFunction() {
            IPEndPoint endpoint = null;
            try
            {
                Console.WriteLine("LFS READ THREAD STARTED");
                while (running)
                {
                    try
                    {
                        byte[] rawData = udpClient.Receive(ref endpoint);


                        float yawOrientation = BitConverter.ToSingle(rawData, 16);
                        float pitchOrientation = BitConverter.ToSingle(rawData, 20);
                        float rollOrientation = BitConverter.ToSingle(rawData, 24);
                        float pitchAcc = BitConverter.ToSingle(rawData, 28);
                        float rollAcc = BitConverter.ToSingle(rawData, 32);
                        float roll_g = (float)((System.Math.Cos(yawOrientation) * pitchAcc) + (System.Math.Sin(yawOrientation) * rollAcc));
                        float pitch_g = (float)((-System.Math.Sin(yawOrientation) * pitchAcc) + (System.Math.Cos(yawOrientation) * rollAcc));

                        controller.SetInput(0, -yawOrientation * 60);


                        controller.SetInput(1, pitchOrientation * 50);
                        controller.SetInput(2, rollOrientation * 50);

                        controller.SetInput(3, pitch_g);
                        controller.SetInput(4, roll_g);

                    }
                    catch (SocketException) { }
                }
            }
            catch (ThreadAbortException) { }
            Console.WriteLine("LFS READ THREAD STOPPED");
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
            return typeof(Config);
        }
    }
}
