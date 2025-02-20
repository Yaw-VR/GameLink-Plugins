using Condor2Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using YawGLAPI;

namespace Condor2Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Condor 2")]
    [ExportMetadata("Version", "1.0")]
    public class Condor2Plugin : Game {

        Thread readThread;
        UdpClient udpClient;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private bool running = false;

        public int STEAM_ID => 0;
        public string AUTHOR => "YawVR";
        public string PROCESS_NAME => string.Empty;
        public bool PATCH_AVAILABLE => true;
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
            running = false;
            udpClient.Close();
            udpClient = null;
        }

        public string[] GetInputData() {
            return new string[] {
                "Yaw","Pitch","Bank","Airspeed","Altitude","Vario","Evario","Nettovario","Integrator","Slipball","Turnrate","Rollrate","Pitchrate","Yawrate","Gforce"
           };
        }

        public void SetReferences(IProfileManager controller,IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init() {
           
            readThread = new Thread(new ThreadStart(ReadFunction));
            running = true;
            readThread.Start();
        }

        private void ReadFunction() {
            try {
                udpClient = new UdpClient(55278);
                Dictionary<string, float> dict = new Dictionary<string, float>();
                byte[] data;
                IPEndPoint endp = null;
                while (running) {
                    data = udpClient.Receive(ref endp);
                  
                    string[] rows = Encoding.ASCII.GetString(data).Split('\n');
                    
                    for(int i =0; i < rows.Length;i++) {
                        if (string.IsNullOrEmpty(rows[i])) continue;
                        float f;
                        string[] row = rows[i].Split('=');
                      
                        float.TryParse(row[1],System.Globalization.NumberStyles.Float,CultureInfo.InvariantCulture ,out f);
                        dict[row[0]] = f;
                    }
                    controller.SetInput(0, dict["yaw"]* 57.2957795f);
                    controller.SetInput(1, dict["pitch"]* 57.2957795f);
                    controller.SetInput(2, dict["bank"]*57.2957795f);
                    controller.SetInput(3, dict["airspeed"]);
                    controller.SetInput(4, dict["altitude"]);
                    controller.SetInput(5, dict["vario"]);
                    controller.SetInput(6, dict["evario"]);
                    controller.SetInput(7, dict["nettovario"]);
                    controller.SetInput(8, dict["integrator"]);
                    controller.SetInput(9, dict["slipball"]);
                    controller.SetInput(10, dict["turnrate"]);
                    controller.SetInput(11, dict["rollrate"]);
                    controller.SetInput(12, dict["pitchrate"]);
                    controller.SetInput(13, dict["yawrate"]);
                    controller.SetInput(14, dict["gforce"]);   
                }
            } catch(ThreadAbortException) { }
        }

        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return null;
        }
        public void PatchGame() {


            string name = "Condor 2";
            System.Reflection.MemberInfo info = typeof(Condor2Plugin);
            foreach (object meta in info.GetCustomAttributes(true)) {
                if (meta is ExportMetadataAttribute) {
                    if (((ExportMetadataAttribute)meta).Name == "Name") {
                        name = (string)((ExportMetadataAttribute)meta).Value;
                    }

                }
            }

            string installPath = dispatcher.GetInstallPath(name);
            if (!Directory.Exists(installPath)) {
                dispatcher.DialogShow("Cant find Condor 2 install directory\nOpen Plugin manager?", DIALOG_TYPE.QUESTION, delegate {
                    dispatcher.OpenPluginManager();
                });
                
                return;
            }

            IniFile file = new IniFile(installPath + "/Settings/UDP.ini");
            file.Write("Enabled", "1", "General");
            file.Write("Port", "55278", "Connection");


           dispatcher.ShowNotification(NotificationType.INFO,"Condor 2 patched!");

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
