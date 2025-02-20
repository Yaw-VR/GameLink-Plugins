using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Vector36Plugin.Properties;
using YawGLAPI;
namespace Vector36Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Vector36")]
    [ExportMetadata("Version", "1.0")]

    public class Vector36Plugin : Game {


        private struct Packets {
            public float yaw,pitch,roll,heave,sway,surge,extra1,extra2,extra3;
        }
            private bool stop = false;
        private Thread readthread;
        UdpClient receivingUdp;
        IPEndPoint RemoteIpEnd = new IPEndPoint(IPAddress.Any, 4123);
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        public int STEAM_ID => 346460;
        public string PROCESS_NAME => "Vector36";
        public bool PATCH_AVAILABLE => true;
        public string AUTHOR => "YawVR";
        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        public LedEffect DefaultLED() {

            return new LedEffect(

                EFFECT_TYPE.KNIGHT_RIDER,
                2,
                new YawColor[] {
                    new YawColor(255, 255, 255),
                    new YawColor(80, 80, 80),
                    new YawColor(255, 255, 0),
                    new YawColor(0, 0, 255),
                },
                0.05f);
        }

        public List<Profile_Component> DefaultProfile() {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit() {
            receivingUdp.Close();
            receivingUdp = null;
            stop = true;
        }

        public string[] GetInputData() {
            Type t = typeof(Packets);
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

            var pConfig = dispatcher.GetConfigObject<Config>();
            receivingUdp = new UdpClient(pConfig.Port);
            readthread = new Thread(new ThreadStart(ReadFunction));
            readthread.Start();
        }

        private void ReadFunction() {
            try {

                Packets p = new Packets();
                FieldInfo[] fields = typeof(Packets).GetFields();
                Console.WriteLine("waiting for packets");
                while (!stop) {
                    byte[] data = receivingUdp.Receive(ref RemoteIpEnd);
                    string stringData = Encoding.ASCII.GetString(data);
                    Console.WriteLine(stringData);
                    string[] array = Strings.Split(stringData, ",");
                    float.TryParse(array[0],out p.roll);
                   
                    float.TryParse(array[1],out p.pitch);
                    float.TryParse(array[2],out p.yaw);
                    float.TryParse(array[3],out p.heave);
                    float.TryParse(array[4],out p.sway);
                    float.TryParse(array[5],out p.surge);
                    float.TryParse(array[6],out p.extra1);
                    float.TryParse(array[7],out p.extra2);
                    float.TryParse(array[8],out p.extra3);



                    for (int i = 0; i < fields.Length; i++) {
                        controller.SetInput(i, (float)fields[i].GetValue(p));
                    }
                }
            }
            catch (SocketException) {
                dispatcher.ExitGame();
            }
        }


        public void PatchGame() {
            string installPath = dispatcher.GetInstallPath("vector36");
         
            Console.WriteLine("Vector36 path is {0} ", installPath);
            if (File.Exists(installPath + "/Vector36.exe")) {
                try {
                    var pConfig = dispatcher.GetConfigObject<Config>();
                    using (StreamWriter streamWriter = new StreamWriter(installPath + "\\Vector36_Data\\StreamingAssets\\telemetry.cfg")) {
                        streamWriter.WriteLine("127.0.0.1");
                        streamWriter.WriteLine(pConfig.Port);
                    }

                    dispatcher.ShowNotification(NotificationType.INFO, "Vector36 patched!");

                }
                catch (Exception ex) {
                    Exception ex2 = ex;
                    Interaction.MsgBox("Error Finding telemetry.cfg.\r\nMake sure it exists in StreamingAssets");

                }
            }
            else {
             dispatcher.ShowNotification(NotificationType.ERROR, "Vector36.exe not found");
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
