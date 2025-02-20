using Gta5Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using YawGLAPI;

namespace Gta5Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Grand Theft Auto V")]
    [ExportMetadata("Version", "1.0")]
    class Gta5Plugin : Game {

        private IPEndPoint senderIP = new IPEndPoint(IPAddress.Any, 0);
        UdpClient udpClient;
        Thread readThread;
        public int STEAM_ID => 271590;
        public bool PATCH_AVAILABLE => true;
        public string AUTHOR => "YawVR";
        public string PROCESS_NAME => string.Empty;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");

        public Stream Background => GetStream("wide.png");

        public string Description => Resources.description;

        public int port = 20777;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        private volatile bool running = false;

        public LedEffect DefaultLED() {

            return new LedEffect(

           EFFECT_TYPE.KNIGHT_RIDER,
           0,
           new YawColor[] {
                    new YawColor(255, 255, 255),
                    new YawColor(80, 80, 80),
                    new YawColor(255, 255, 0),
                    new YawColor(0, 0, 255),
           },
           0.5f);
        }

        public List<Profile_Component> DefaultProfile() {
            return new List<Profile_Component>() {

                new Profile_Component(1,1, 1.0f,1.0f,0f,false,true,-1f,1.0f),
                new Profile_Component(2,2, 0.7f,0.7f,0f,false,true,-1f,1.0f),
                new Profile_Component(10,1, 30.0f,30.0f,0f,false,true,-1f,0.08f),
                new Profile_Component(3,0, 1.0f,1.0f,0f,false,true,-1f,1.0f),
                new Profile_Component(5,4, 23.0f,23.0f,0f,false,false,-1f,1.0f),
                new Profile_Component(0,3, 8.0f,8.0f,0f,true,false,-1f,1.0f)
            };
        }

        public void Exit() {

            udpClient.Close();
            udpClient = null;
            running = false;
            //readThread.Abort();
        }
     
        public string[] GetInputData() {

            Type t = typeof(Packets);
            FieldInfo[] fields = t.GetFields();

            string[] inputs = new string[fields.Length];

            for(int i =0;i<fields.Length;i++) {
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

            udpClient = new UdpClient(20777);
            udpClient.Client.ReceiveTimeout = 5000;
            running = true;
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }

        private void ReadFunction() {

            Packets sende = new Packets();
            FieldInfo[] fields =  typeof(Packets).GetFields();
            while (running) {
                try
                {
                    byte[] rawData = udpClient.Receive(ref senderIP);



                    IntPtr unmanagedPointer =
                        Marshal.AllocHGlobal(rawData.Length);
                    Marshal.Copy(rawData, 0, unmanagedPointer, rawData.Length);
                    // Call unmanaged code
                    Marshal.FreeHGlobal(unmanagedPointer);
                    Marshal.PtrToStructure(unmanagedPointer, sende);


                    for (int i = 0; i < fields.Length; i++)
                    {
                        controller.SetInput(i, (float)fields[i].GetValue(sende));
                    }
                } catch(SocketException) { }
            }
        }



        public void PatchGame() {
            string name = "gta";
            System.Reflection.MemberInfo info = typeof(Gta5Plugin);
            foreach (object meta in info.GetCustomAttributes(true)) {
                if (meta is ExportMetadataAttribute) {
                    if (((ExportMetadataAttribute)meta).Name == "Name") {
                        name = (string)((ExportMetadataAttribute)meta).Value;
                    }

                }
            }
          


                string tempPath = Path.GetTempFileName();
            string installPath = dispatcher.GetInstallPath(name);
            if (!Directory.Exists(installPath)) {
                dispatcher.DialogShow("Cant find GTA install directory\nOpen Plugin manager?", DIALOG_TYPE.QUESTION, delegate {
                    dispatcher.OpenPluginManager();
                });
                return;
            }

            Console.WriteLine("Downloading http://yaw.one/gameengine/Plugins/Grand_Theft_Auto_V/Gta5files.zip to " + tempPath);
            using (WebClient wc = new WebClient()) {
                wc.DownloadFile("http://yaw.one/gameengine/Plugins/Grand_Theft_Auto_V/Gta5files.zip", tempPath);
            }


          
            Console.WriteLine("Metadata Name: " + name + ", Installpath:" + installPath);


            dispatcher.ExtractToDirectory(tempPath,installPath,true);
         

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
