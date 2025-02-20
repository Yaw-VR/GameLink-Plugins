using FernbusPlugin.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Fernbus Simulator")]
    [ExportMetadata("Version", "1.0")]
    public class FernbusPlugin : Game {

        public struct Packet {
            public float yaw, pitch, roll;
            public float speed;
            public float rpmPercent;
            public float steering, throttle, brake;
            public float engineStarted;
            public float isOffroad;
        }
        private bool stop = false;
        private Thread readthread;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        public string PROCESS_NAME => "Fernbus-Win64-Shipping";
        private static readonly string JSON_URL = "http://localhost:37337/Vehicles/Current";

        public int STEAM_ID => 427100;
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "YawVR";


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
            stop = true;
            //readthread.Abort();

        }

        public string[] GetInputData() {
            Type t = typeof(Packet);
            FieldInfo[] fields = t.GetFields();

            string[] inputs = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++) {
                inputs[i] = fields[i].Name;
            }
            return inputs;
        }
 
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher) {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
        public void Init() {

            stop = false;
            readthread = new Thread(new ThreadStart(ReadFunction));
            readthread.Start();
        }

        private void ReadFunction() {
            using (WebClient wc = new WebClient()) {
                Packet p = new Packet();
                FieldInfo[] fields = typeof(Packet).GetFields();
                while (!stop) {
                    try {
                        string data = wc.DownloadString(JSON_URL);

                        var json = JsonConvert.DeserializeObject<dynamic>(data);
                        p.speed = json.Speed;

                        p.yaw = json.Rotation.Yaw;
                        p.pitch = json.Rotation.Pitch;
                        p.roll = json.Rotation.Roll;

                        if (json.MaxRPM != 0) p.rpmPercent = json.RPM / json.MaxRPM;

                        p.steering = json.Steering;
                        p.throttle = json.Throttle;
                        p.brake = json.Brake;

                        p.engineStarted = Convert.ToSingle((bool)json.EngineStarted);
                        p.isOffroad = Convert.ToSingle((bool)json.IsOffroad);


                        for (int i = 0; i < fields.Length; i++) {
                            controller.SetInput(i, (float)fields[i].GetValue(p));
                        }
                    } catch (WebException) {
                        Thread.Sleep(5000);
                    }
                    Thread.Sleep(20);


                }
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
