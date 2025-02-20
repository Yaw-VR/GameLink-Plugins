using ProjectCars2Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using YawGLAPI;

namespace ProjectCars2Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Project Cars 2")]
    [ExportMetadata("Version", "1.0")]
    public class ProjectCars2Plugin : Game {

        UdpClient udpClient;
        private volatile bool running = false;
        private Thread readthreade;

        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 5606);
        public string PROCESS_NAME => "pCARS2AVX";
        public int STEAM_ID => 378860;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => false;

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        public IMainFormDispatcher dispatcher { get; private set; }

        private IProfileManager controller;
        private IPEndPoint groupEP;
        PCars2_UDP uDP;           //Create an UDP object that will retrieve telemetry values from in game.


        public void PatchGame()
        {
            return;
        }

        public LedEffect DefaultLED() {
            return new LedEffect(

           EFFECT_TYPE.KNIGHT_RIDER,
           7,
           new YawColor[] {
                new YawColor(66, 135, 245),
                new YawColor(80,80,80),
                new YawColor(128, 3, 117),
                new YawColor(110, 201, 12),
                },
           0.003f);
        }
        public List<Profile_Component> DefaultProfile() {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit() {
            udpClient.Close();
            udpClient = null;
            running = false;
           // readthreade.Abort();
        }

        public string[] GetInputData() {
            return new string[] {

                "Yaw","Pitch","Roll","RPM","Speed","Pitch_acc","Roll_acc","Throttle","Brake","VelocityX","VelocityY","VelocityZ","AngularX","AngularY","AngularZ","SuspensionV0","SuspensionV1","SuspensionV2","SuspensionV3"
            };
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

     
        public void Init() {
            Console.WriteLine("ProjectCars2 INIT");

            var pConfig = dispatcher.GetConfigObject<Config>();
            running = true;
            udpClient = new UdpClient(pConfig.Port);
            udpClient.Client.ReceiveTimeout = 5000;
            groupEP = new IPEndPoint(IPAddress.Any, pConfig.Port);
            uDP = new PCars2_UDP(udpClient, groupEP);
            readthreade = new Thread(new ThreadStart(ReadFunction));
            readthreade.Start();
        }

        private void ReadFunction() {
            Console.WriteLine("ProjectCars2 RD");
            while (running) {
                try
                {


                    uDP.readPackets();                      //Read Packets ever loop iteration



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

                 
                    float x = uDP.LocalAcceleration[2];

                    // controller.SetInput(20, x * (10/(x/10)));
                    //     controller.SetInput(20, currentPitchacc);

                  



                } catch(SocketException) { }

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
            return typeof(Config);
        }
    }
}
