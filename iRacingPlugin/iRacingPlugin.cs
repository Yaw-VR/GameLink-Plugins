using iRacingSDK;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin {
    [Export(typeof(Game))]
    [ExportMetadata("Name", "iRacing")]
    [ExportMetadata("Version", "1.1")]
    public class iRacingPlugin : Game {

        private Thread readthreade;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        private volatile bool running = false;
        public int STEAM_ID => 266410;

        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "YawVR";
        public string PROCESS_NAME => string.Empty;

        public string Description => string.Empty;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        public LedEffect DefaultLED() {
            return new LedEffect(

           EFFECT_TYPE.KNIGHT_RIDER_2,
           3,
           new YawColor[] {
                new YawColor(66, 135, 245),
                 new YawColor(80,80,80),
                new YawColor(128, 3, 117),
                new YawColor(110, 0, 12),
                },
           1f);

        }

        public List<Profile_Component> DefaultProfile() {
            return new List<Profile_Component>() {
                new Profile_Component(0,0, 1,1,0f,false,true,-1,1f), //yaw
                new Profile_Component(1,1, 1,1,0f,false,false,-1,1f), //pitch
                new Profile_Component(2,2, 1,1,0f,false,true,-1,1f), //roll

                new Profile_Component(6,1, 1,1,0f,false,true,-1,1f), //yaw
                new Profile_Component(7,2, 0.6f,0.6f,0f,false,true,-1,1f), //yaw
            };
        }

        public void Exit() {
            running = false;
           // readthreade.Abort();
        }

        public string[] GetInputData() {
            return new string[] {
                "Yaw","Pitch","Roll","RPM","Speed","Acc_vertical","Acc_long","Acc_lateral","Velocity_x","Velocity_y","Velocity_z",
                "L-FrontShockDeflection","R-FrontShockDeflection","L-RearShockDeflection","R-RearShockDeflection",
                "L-Front_shock_Velocity","R-Front_shock_Velocity","L-Rear_shock_Velocity","R-Rear_shock_Velocity",
                "C-FrontShockDeflection","C-RearShockDeflection","C-Front_shock_Velocity","C-Rear_shock_Velocity"
            };

        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
        public void Init() {
            Console.WriteLine("iRacing INIT");
            running = true;
            readthreade = new Thread(new ThreadStart(ReadFunction));
            readthreade.Start();
        }
        public void PatchGame()
        {
            return;
        }

        private void ReadFunction() {
            try {
                var iracing = new iRacingConnection();
           
                    while (running) {
                  
                        var data = iracing.GetDataFeed().First();
                        if (!data.IsConnected) continue; 

                        controller.SetInput(0, data.Telemetry.Yaw * 57.2957795f);
                        controller.SetInput(1, data.Telemetry.Pitch * 57.2957795f);
                        controller.SetInput(2, data.Telemetry.Roll * 57.2957795f);
                        controller.SetInput(3, data.Telemetry.RPM / 1500);
                        controller.SetInput(4, data.Telemetry.Speed);
                        controller.SetInput(5, data.Telemetry.VertAccel);
                        controller.SetInput(6, data.Telemetry.LongAccel);
                        controller.SetInput(7, data.Telemetry.LatAccel);
                        controller.SetInput(8, data.Telemetry.VelocityX);
                        controller.SetInput(9, data.Telemetry.VelocityY);
                        controller.SetInput(10, data.Telemetry.VelocityZ);
                        controller.SetInput(11, data.Telemetry.LFshockDefl);
                        controller.SetInput(12, data.Telemetry.RFshockDefl);
                        controller.SetInput(13, data.Telemetry.LRshockDefl);
                        controller.SetInput(14, data.Telemetry.RRshockDefl);
                        controller.SetInput(15, data.Telemetry.LFshockVel);
                        controller.SetInput(16, data.Telemetry.RFshockVel);
                        controller.SetInput(17, data.Telemetry.LRshockVel);
                        controller.SetInput(18, data.Telemetry.RRshockVel);
                        controller.SetInput(19, data.Telemetry.CFshockDefl);
                        controller.SetInput(20, data.Telemetry.CRshockDefl);
                        controller.SetInput(21, data.Telemetry.CFshockVel);
                        controller.SetInput(22, data.Telemetry.CRshockVel);



                }
                
            }
            catch (ThreadAbortException) { }
            catch (Exception ex) {
                
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

