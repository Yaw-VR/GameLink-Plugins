using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using TheCrew2.Properties;
using YawGLAPI;

namespace TheCrew2
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "The Crew 2")]
    [ExportMetadata("Version", "1.0")]
    public class TheCrew2Plugin : Game {


        private volatile bool running = false;
        private Thread readthread;
        UdpClient receivingUdpClient;
        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        public int STEAM_ID => 646910;
        public bool PATCH_AVAILABLE => true;
        public string AUTHOR => "YawVR";
        public string PROCESS_NAME => "";


        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        public LedEffect DefaultLED() {
            return new LedEffect(

                EFFECT_TYPE.KNIGHT_RIDER_2,
                 7,
                new YawColor[] {
                new YawColor(66, 135, 245),
                new YawColor(80,80,80),
                new YawColor(128, 3, 117),
                new YawColor(110, 201, 12),
                  },
                    25f);
        }
      
        public List<Profile_Component> DefaultProfile() {
            return new List<Profile_Component>() {
                new Profile_Component(4,1, 1,0f,0f,false,false,-1,1f),
                new Profile_Component(7,1, 0.5f,0f,0f,false,true,-1,0.30f),
                new Profile_Component(1,2, 0.1f,0f,0f,false,false,-1,0.05f),
                new Profile_Component(5,2, 1,0f,0f,false,true,-1,1f),
                new Profile_Component(0,1, 0.1f,0f,0f,false,false,-1,1f),
                new Profile_Component(3,0, 1,0f,0f,false,true,-1,1f),
            };
        }

        public void Exit() {
            receivingUdpClient.Close();
            receivingUdpClient = null;
            running = false;
            //readthread.Abort();
        }

        public string[] GetInputData() {
            return new string[] {
                "AngularVelocityP","AngularVelocityR","AngularVelocityY","OrientationYaw","OrientationPitch",
                "OrientationRoll","Acceleration_Lateral","Acceleration_longitudinal","Acceleration_vertical",
                "VelocityX","VelocityY","VelocityZ","PositionX","PositionY","PositionZ"
            };
        }

       
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

        public void Init() {

            var pConfig = dispatcher.GetConfigObject<Config>();

            running = true;
            receivingUdpClient = new UdpClient(pConfig.Port);
            readthread = new Thread(new ThreadStart(ReadFunction));
            readthread.Start();
        }

        private void ReadFunction() {

            while (running) {
                byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                float AngularVelocityP = BitConverter.ToSingle(receiveBytes, 4);
                float AngularVelocityR = BitConverter.ToSingle(receiveBytes, 8);
                float AngularVelocityY = BitConverter.ToSingle(receiveBytes, 12);
                float OrientationYaw = BitConverter.ToSingle(receiveBytes, 16);
                float OrientationPitch = BitConverter.ToSingle(receiveBytes, 20);
                float OrientationRoll = BitConverter.ToSingle(receiveBytes, 24);
                float Acceleration_Lateral = BitConverter.ToSingle(receiveBytes, 28);
                float Acceleration_longitudinal = BitConverter.ToSingle(receiveBytes, 32);
                float Acceleration_vertical = BitConverter.ToSingle(receiveBytes, 36);
                float VelocityX = BitConverter.ToSingle(receiveBytes, 40);
                float VelocityY = BitConverter.ToSingle(receiveBytes, 44);
                float VelocityZ = BitConverter.ToSingle(receiveBytes, 48);
                float PositionX = BitConverter.ToSingle(receiveBytes, 52);
                float PositionY = BitConverter.ToSingle(receiveBytes, 56);
                float PositionZ = BitConverter.ToSingle(receiveBytes, 60);
                float GameID = BitConverter.ToSingle(receiveBytes, 64);

                controller.SetInput(0,AngularVelocityP * 57.295f);
                controller.SetInput(1,AngularVelocityR * 57.295f);
                controller.SetInput(2,AngularVelocityY * 57.295f);
                controller.SetInput(3,OrientationYaw * 57.295f);
                controller.SetInput(4,OrientationPitch * 57.295f);
                controller.SetInput(5,OrientationRoll * 57.295f);
                controller.SetInput(6,Acceleration_Lateral);
                controller.SetInput(7,Acceleration_longitudinal);
                controller.SetInput(8,Acceleration_vertical);
                controller.SetInput(9,VelocityX);
                controller.SetInput(10,VelocityY);
                controller.SetInput(11,VelocityZ);
                controller.SetInput(12,PositionX);
                controller.SetInput(13,PositionY);
                controller.SetInput(14,PositionZ);
                

                
            }
        }
        public void PatchGame() {
            try {
                string DocumentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "/The Crew 2";

                //string[] dirs = Directory(@DocumentsPath + "The Crew 2", SearchOption.TopDirectoryOnly);
                Console.WriteLine(DocumentsPath);
              

                // Call the methods.
                

                string tempPath = Path.GetTempFileName();
                string installPath = dispatcher.GetInstallPath(DocumentsPath);
                

                using (WebClient wc = new WebClient()) {
                    wc.DownloadFile("http://yaw.one/gameengine/Plugins/The_Crew_2/TheCrew2.zip", tempPath);
                    Console.WriteLine(DocumentsPath);
                    dispatcher.ExtractToDirectory(tempPath, DocumentsPath,true);
                    
                }
            }
            catch (Exception e) {
                Console.WriteLine("{0}", e.ToString());
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
