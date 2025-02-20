using ProjectCars3Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using YawGLAPI;

namespace ProjectCars3Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Project Cars 3")]
    [ExportMetadata("Version", "1.0")]
    public class ProjectCars3Plugin : Game {

        
        private volatile bool running = false;
        private Thread readthread;

        public string PROCESS_NAME => "pCARS3";
        public int STEAM_ID => 958400;
        public bool PATCH_AVAILABLE => false;

        public string AUTHOR => "YawVR";
        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");




        UdpClient listener;            //Create a UDPClient object
        IPEndPoint groupEP;  //Start recieving data from any IP listening on port 5606 (port for PCARS2)

        PCars2_UDP uDP;           //Create an UDP object that will retrieve telemetry values from in game.
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

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
           36f);
        }
        public List<Profile_Component> DefaultProfile() {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit() {
            //stop = true;
            // readthread.Abort();
            running = false;
            listener.Close();
            listener = null;

        }

        public string[] GetInputData() {
            return new string[] {

                "Yaw","Pitch","Roll","RPM","Speed","Pitch_acc","Roll_acc","Throttle","Brake","VelocityX","VelocityY","VelocityZ","AngularX","AngularY","AngularZ","SuspensionV0","SuspensionV1","SuspensionV2","SuspensionV3","CRASH"
            };
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

        public string GetDescription()
        {
            return Resources.description;
        }

        public void Init() {
            Console.WriteLine("ProjectCars3 INIT");
            running = true;

            var pConfig = dispatcher.GetConfigObject<Config>();
            listener = new UdpClient(pConfig.Port);
            listener.Client.ReceiveTimeout = 5000;
            groupEP  = new IPEndPoint(IPAddress.Any, pConfig.Port);
            uDP = new PCars2_UDP(listener, groupEP);

            readthread = new Thread(new ThreadStart(ReadFunction));
            readthread.Start();
        }

        private void ReadFunction() {

            float previousSpeed = 0f;
            float crash = 0f;
            while (running) {
                try
                {
                    uDP.readPackets();                      //Read Packets ever loop iteration

                    if (Math.Abs(uDP.LocalVelocity[2] - previousSpeed) > 8)
                    {
                        crash = (float)(Math.Sign(uDP.LocalVelocity[2] - previousSpeed)) * 10f;
                    }
                    crash = Lerp(crash, 0, 0.01f);
                    if (Math.Abs(crash) < 1) crash = 0;

                    controller.SetInput(0, uDP.Orientation[1] * 57.2957795f);
                    controller.SetInput(1, uDP.Orientation[0] * 57.2957795f);
                    controller.SetInput(2, uDP.Orientation[2] * 57.2957795f);
                    if (uDP.MaxRpm != 0) controller.SetInput(3, (float)uDP.Rpm / (float)uDP.MaxRpm);
                    controller.SetInput(4, uDP.Speed);

                    controller.SetInput(5, uDP.LocalAcceleration[2]);
                    controller.SetInput(6, uDP.LocalAcceleration[0]);

                    controller.SetInput(7, uDP.Throttle);
                    controller.SetInput(8, uDP.Brake);

                    controller.SetInput(9, uDP.LocalVelocity[0]);
                    controller.SetInput(10, uDP.LocalVelocity[1]);
                    controller.SetInput(11, uDP.LocalVelocity[2]);

                    controller.SetInput(12, uDP.AngularVelocity[0]);
                    controller.SetInput(13, uDP.AngularVelocity[1]);
                    controller.SetInput(14, uDP.AngularVelocity[2]);


                    controller.SetInput(15, uDP.SuspensionVelocity[0]);
                    controller.SetInput(16, uDP.SuspensionVelocity[1]);
                    controller.SetInput(17, uDP.SuspensionVelocity[2]);
                    controller.SetInput(18, uDP.SuspensionVelocity[3]);

                    controller.SetInput(19, crash);
                    float x = uDP.LocalAcceleration[2];

                    // controller.SetInput(20, x * (10/(x/10)));
                    //     controller.SetInput(20, currentPitchacc);

                    previousSpeed = uDP.LocalVelocity[2];
                } catch(SocketException) { }
            }
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
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
