using AeroFS2Plugin.Properties;
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

namespace YawVR_Game_Engine.Plugin {

    [Export(typeof(Game))]
    [ExportMetadata("Name", "Aerofly FS 2")]
    [ExportMetadata("Version", "1.1")]
    class AeroFS2Plugin : Game {
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private UdpClient udpClient;
        private Thread readThread;

        private float absYaw = 0;
        private bool running = false;

        public int STEAM_ID => 434030;
        public bool PATCH_AVAILABLE => true;
        public string PROCESS_NAME => "aerofly_fs_2";
        public string AUTHOR => "YawVR";

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");
        public string Description => Resources.description;



        public LedEffect DefaultLED() {
            return new LedEffect(
                EFFECT_TYPE.FLOW_LEFTRIGHT,
              4,
               new YawColor[] {
                new YawColor(190, 250, 192),
                new YawColor(80,80,80),
                new YawColor(255, 105, 36),
                new YawColor(80,80,80) },
               3f);
        }

        public List<Profile_Component> DefaultProfile() {
            return new List<Profile_Component>() {
                 new Profile_Component(1,1, 0.5f, 0.5f,0f,false,true,-1,1f),
                new Profile_Component(2,2, 0.5f,0.5f,0f,false,true,-1,1f),
                new Profile_Component(0,0, 1,1,0f,false,false,-1,1f),
                new Profile_Component(3,1, 1,1,0f,false,false,-1,1f),
                new Profile_Component(4,2, 1,1,0f,false,true,-1,1f),

            };
        }

        public void Exit() {
            try {
                running = false;
                readThread = null;
                udpClient.Close();
                udpClient = null;
            } catch { }
        }

        public string[] GetInputData() {
            return new string[] {
                "Yaw","Pitch","Bank","Force_Pitch","Force_Roll"
            };
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init() {
         
            udpClient = new UdpClient(4123);
            readThread = new Thread(new ThreadStart(ReadFunction));

            running = true;
            readThread.Start();
        }

        private void ReadFunction() {
            Console.WriteLine("AEROFLY2 READ THREAD STARTED");
            IPEndPoint endpoint = null;
            while(running) {
                try
                {
                    byte[] rawData = udpClient.Receive(ref endpoint);
                    string[] data = Encoding.ASCII.GetString(rawData).Split(';');

                    //orientáció

                    //  float yaw = float.Parse(data[2],CultureInfo.InvariantCulture) / 1000 * 57.3f;
                    float pitch_O = float.Parse(data[0], CultureInfo.InvariantCulture) / 2000 * 57.3f;
                    float bank_O = float.Parse(data[1], CultureInfo.InvariantCulture) / 2000 * 57.3f;


                    float yaw = -float.Parse(data[2], CultureInfo.InvariantCulture) / 350;
                    float pitch_g = float.Parse(data[4], CultureInfo.InvariantCulture) / 100;
                    float roll_g = float.Parse(data[3], CultureInfo.InvariantCulture) / 150;

                    absYaw += yaw;

                    controller.SetInput(0, absYaw);
                    controller.SetInput(1, pitch_O);
                    controller.SetInput(2, bank_O);
                    controller.SetInput(3, pitch_g);
                    controller.SetInput(4, roll_g);
                } catch(Exception ex) { }
            }
        }
     
        public void PatchGame() {

            using (WebClient wc = new WebClient()) {
                string sgPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Aerofly FS 2\\external_dll");

                if (!Directory.Exists(sgPath)) {
                    dispatcher.DialogShow("Cant find Aerofly FS 2 external_dll folder!\nMake sure to start Aerofly FS 2 at least once before patching!",DIALOG_TYPE.INFO);
                    return;
                }
                wc.DownloadFile("http://yaw.one/gameengine/Plugins/Aerofly_FS_2/Aerofly_FS_2_GamePlugin_Telemetry.dll",sgPath+ "/Aerofly_FS_2_GamePlugin_Telemetry.dll");

               dispatcher.ShowNotification(NotificationType.INFO,"Aerofly FS 2 patched!");

            }

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
