using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using VRaceHoverBikePlugin.Properties;
using YawGLAPI;

namespace VRaceHoverBikePlugin
{


    [Export(typeof(Game))]
    [ExportMetadata("Name", "VRace Hover Bike")]
    [ExportMetadata("Version", "1.0")]
    

    public class VRaceHoverBikePlugin : Game {
        private UdpClient udpClient;
        private Thread readThread;
        private bool stopThread;
        private IPEndPoint remotepoint;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        public bool PATCH_AVAILABLE => true;

        public string PROCESS_NAME => "vracer_hoverbike";
        public int STEAM_ID => 668430;
        public string AUTHOR => "YawVR";

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        public LedEffect DefaultLED() {
            return new LedEffect(
            EFFECT_TYPE.KNIGHT_RIDER,
              3,
              new YawColor[] {
                new YawColor(66, 135, 245),
                new YawColor(80,80,80),
                new YawColor(128, 3, 117),
                new YawColor(110, 201, 12),
                },
              0.7f);
        }

        public List<Profile_Component> DefaultProfile() {
            return new List<Profile_Component> {
                new Profile_Component(9,1, 0.4f,0.4f,0f,false,false,-1,1f),
                new Profile_Component(7,1, 0.4f,0.4f,0f,false,false,-1,1f),
                new Profile_Component(0,3, 0.1f,0.1f,0f,false,false,-1,1f),
                new Profile_Component(9,4, 0.4f,0.4f,0f,true,false,-1,1f),
            };
        }

        public void Exit() {
            udpClient.Close();
            udpClient = null;
            stopThread = true;
        }

        public string[] GetInputData() {
            Type t = typeof(TelemetryData);
            FieldInfo[] fields = t.GetFields();

            string[] inputs = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++) {
                inputs[i] = fields[i].Name;
            }
            return inputs;
        }

      
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

      
        public void Init() {
            stopThread = false;
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }

        private void ReadFunction() {

            var pConfig = dispatcher.GetConfigObject<Config>();
            udpClient = new UdpClient(pConfig.Port);
            FieldInfo[] fields = typeof(TelemetryData).GetFields();
     
            while (!stopThread) {

                try {
                    byte[] rawData = udpClient.Receive(ref remotepoint);
                    string receive = Encoding.ASCII.GetString(rawData);
                    TelemetryData data = JsonConvert.DeserializeObject<TelemetryData>(receive);

                    for (int i = 0; i < fields.Length; i++) {
                        controller.SetInput(i, (float)fields[i].GetValue(data));
                    }

                }
                catch (Exception e) {

                    Console.WriteLine(e.Message); ;
                }

            }
        }

        public void PatchGame() {
            
            try {
                string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"/../Locallow\VertexBreakers\vracer_hoverbike";
                Console.WriteLine(Directory.Exists(folder));
                
                using (StreamWriter sr = new StreamWriter(folder + "/telemetry.json")) {
                    var pConfig = dispatcher.GetConfigObject<Config>();
                    string json = "{ \"ip\":\"127.0.0.1\", \"port\": "+pConfig+", \"refreshRate\":"+pConfig.RefreshRate+" }";
                    sr.WriteLine(json);
                }
            }
            catch (Exception e) {

                Console.WriteLine(e.ToString());
            }
            
        }

        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return null;
        }

        [System.Serializable]
        public class TelemetryData {
            public float speed;
            public float accelerationX, accelerationY, accelerationZ;
            public float angularSpeedX, angularSpeedY, angularSpeedZ;
            public float rotationX, rotationY, rotationZ;
            #region States
          //  public bool paused;
           // public bool onTrack;
            public float gear;
            public float rpm;
            #endregion
            #region events
          //  public bool collectPickup;
          //  public bool turbo;
          //  public bool startEngine;
           // public bool stopEngine;
        //    public bool collision;
            public float collisionAngle;
            #endregion
            float STEP = 0.01f;
    
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