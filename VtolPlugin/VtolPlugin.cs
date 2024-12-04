﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using VtolPlugin.Properties;
using YawGLAPI;

namespace YawVR_Game_Engine
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "VTOL VR")]
    [ExportMetadata("Version", "1.0")]

    public class VtolPlugin : Game {

        
        private bool stop = false;
        private Thread readthread;
        UdpClient receivingUdpClient;
        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 4123);
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;


        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => true;
        public int STEAM_ID => 667970;
        public string PROCESS_NAME => string.Empty;


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
            receivingUdpClient.Close();
            receivingUdpClient = null;
            stop = true;
        }

        public string[] GetInputData() {
            return new string[] {
                "Yaw","Pitch","Roll","Xacceleration","Yacceleration","Zacceleration","AirSpeed","VerticalSpeed","AoA"
            };
        }
        public string GetDescription()
        {
            return Resources.description;
        }
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init() {
            receivingUdpClient = new UdpClient(4123);
            readthread = new Thread(new ThreadStart(ReadFunction));
            readthread.Start();
        }

        private void ReadFunction() {
            while (!stop) {
                try
                {
                    LogData f_info = new LogData();
                    byte[] data = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                   // Console.WriteLine(data[0]);
                    string receive = Encoding.ASCII.GetString(data);
                   // Debug.WriteLine(receive);
                    LogData datas = JsonConvert.DeserializeObject<LogData>(receive);

                    controller.SetInput(0, datas.Vehicle.Heading);
                    controller.SetInput(1, datas.Vehicle.Pitch);
                    controller.SetInput(2, datas.Vehicle.Roll);
                    controller.SetInput(3, datas.Physics.XAccel);
                    controller.SetInput(4, datas.Physics.YAccel);
                    controller.SetInput(5, datas.Physics.ZAccel);
                    controller.SetInput(6, datas.Vehicle.Airspeed);
                    controller.SetInput(7, datas.Vehicle.VerticalSpeed);
                    controller.SetInput(8, datas.Vehicle.AoA);

                } catch(SocketException ex)
                {
                    Console.WriteLine("VTOL: " + ex);
                }

                 catch(JsonSerializationException) { }
               
              
            }
        }


        Stream GetStream(string resourceName)
        {
            var assembly = GetType().Assembly;
            var rr = assembly.GetManifestResourceNames();
            string fullResourceName = $"{assembly.GetName().Name}.Resources.{resourceName}";
            return assembly.GetManifestResourceStream(fullResourceName);
        }

        public void PatchGame() {
            string name = "Vtol VR";
            string installPath = dispatcher.GetInstallPath(name);
            Console.WriteLine(installPath);
            if (!Directory.Exists(installPath)) {
                dispatcher.DialogShow("Cant find Vtol VR install directory\nOpen Plugin manager?", DIALOG_TYPE.QUESTION, delegate {
                    dispatcher.OpenPluginManager();
                });
                return;
            }

            try {
                string tempPath = Path.GetTempFileName();
                     
                using (WebClient wc = new WebClient()) {
                    wc.DownloadFile("http://yaw.one/gameengine/Plugins/VTOL_VR/VTOLVR_ModLoader.zip", tempPath);
                    Console.WriteLine(name , installPath);
                    dispatcher.ExtractToDirectory(tempPath, installPath,true);
                }
            }
            catch (Exception e) {

                Console.WriteLine(e.Message);
            }




        }
        
     
        public class LogData {
            public string Location { get; set; }
           // public Int32 unixTimestamp { get; set; }

            public VehicleClass Vehicle { get; set; } = new VehicleClass();
            public PhysicsClass Physics { get; set; } = new PhysicsClass();

            public class PhysicsClass {
                public float XAccel { get; set; }
                public float YAccel { get; set; }
                public float ZAccel { get; set; }
                public string PlayerGs { get; set; }
            }

            public class VehicleClass {
              //  public string IsLanded { get; set; }
              //  public string Drag { get; set; }
              //  public string Mass { get; set; }
              //  public string VehicleName { get; set; }
              public float Airspeed { get; set; }
              public float VerticalSpeed { get; set; }
              public float AltitudeASL { get; set; }
                public float Heading { get; set; }
                public float Pitch { get; set; }
                public float AoA { get; set; }
                public float Roll { get; set; }
                public string TailHook { get; set; }
               // public string Health { get; set; }
               // public string Flaps { get; set; }
              //  public string Brakes { get; set; }
              //  public string GearState { get; set; }
               // public string RadarCrossSection { get; set; }
                public string BatteryLevel { get; set; }
                public List<Dictionary<string, string>> Engines { get; set; }
                public string EjectionState { get; set; }
                public Dictionary<string, string> Lights { get; set; }


                public FuelClass Fuel { get; set; } = new FuelClass();
                public AvionicsClass Avionics { get; set; } = new AvionicsClass();


                public class AvionicsClass {
                    public string StallDetector { get; set; }
                    public string MissileDetected { get; set; }
                    public string RadarState { get; set; }
                    public List<Dictionary<string, string>> RWRContacts { get; set; }
                    public string masterArm { get; set; }

                }

                public class FuelClass {
                    public string FuelLevel { get; set; }
                    public string FuelBurnRate { get; set; }
                    public string FuelDensity { get; set; }
                }

            }
        }
        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return null;
        }
    }
}
      
        


