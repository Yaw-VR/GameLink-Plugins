using IL2Plugin;
using IL2Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin {
    [Export(typeof(Game))]
    [ExportMetadata("Name", "IL2")]
    [ExportMetadata("Version", "1.0")]

    class IL2Plugin : Game {

        UdpClient udpClient;
        private bool stop = false;
        private Thread readThread;
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 4321);
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        public int STEAM_ID => 307960;

        public string AUTHOR => "YawVR";
        public string PROCESS_NAME => string.Empty;
        public bool PATCH_AVAILABLE => false;

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        public LedEffect DefaultLED() {
            return new LedEffect(

           EFFECT_TYPE.FLOW_LEFTRIGHT,
           2,
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
                new Profile_Component(0,0, 1,1,0f,false,false,-1,1f), //yaw
                new Profile_Component(1,1, 1,1,0f,false,false,-1,1f), //pitch
                new Profile_Component(2,2, 1,1,0f,false,true,-1,1f), // roll

                new Profile_Component(3,2, 1,1,0f,false,true,-1,1f), // roll
                new Profile_Component(4,1, 1,1,0f,false,true,-1,1f), // pitch
            };
        }

        public void Exit() {
            udpClient.Close();
            udpClient = null;
            stop = true;
          //  readThread.Abort();
        }

        public string[] GetInputData() {
            return new string[] {
                "Yaw","Pitch","Roll","Velocity_X","Velocity_Y","Velocity_Z","Acceleration_X","Acceleration_Y","Acceleration_Z"
            };
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
        public void Init() {
            Console.WriteLine("IL2 INIT");
            stop = false;
            var pConfig = dispatcher.GetConfigObject<Config>();
            udpClient = new UdpClient(pConfig.Port);
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();

        }


        
        private void ReadFunction() {

            while (!stop) {

                try
                {
                    byte[] rawData = udpClient.Receive(ref remote);

                    float yaw = ReadSingle(rawData, 8, true) * 57.3f;
                    float pitch = ReadSingle(rawData, 12, true) * 57.3f;
                    float roll = ReadSingle(rawData, 16, true) * 57.3f;

                    float velocityX = ReadSingle(rawData, 20, true) * 57.3f;
                    float velocityY = ReadSingle(rawData, 24, true) * 57.3f;
                    float velocityZ = ReadSingle(rawData, 28, true) * 57.3f;


                    float accX = ReadSingle(rawData, 32, true);
                    float accY = ReadSingle(rawData, 36, true);
                    float accZ = ReadSingle(rawData, 40, true);

                    controller.SetInput(0, yaw);
                    controller.SetInput(1, pitch);
                    controller.SetInput(2, roll);

                    controller.SetInput(3, velocityX);
                    controller.SetInput(4, velocityY);
                    controller.SetInput(5, velocityZ);


                    controller.SetInput(6, accX);
                    controller.SetInput(7, accY);
                    controller.SetInput(8, accZ);
                } catch(SocketException) { }
            }
        }

        public static float ReadSingle(byte[] data, int offset, bool littleEndian)
        {
            if (BitConverter.IsLittleEndian != littleEndian)
            {   // other-endian; reverse this portion of the data (4 bytes)
                byte tmp = data[offset];
                data[offset] = data[offset + 3];
                data[offset + 3] = tmp;
                tmp = data[offset + 1];
                data[offset + 1] = data[offset + 2];
                data[offset + 2] = tmp;
            }
            return BitConverter.ToSingle(data, offset);
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

