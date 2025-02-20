using Dirtrally2Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Xml;
using YawGLAPI;

namespace Dirtrally2Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Dirt Rally 2")]
    [ExportMetadata("Version", "1.0")]
    class DirtRally2Plugin : Game {
        UdpClient udpClient;


        Thread readThread;
        private IPEndPoint remoteIP;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;

        public string PROCESS_NAME => "dirtrally2";
        public int STEAM_ID => 690790;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => true;

        public string Description => Resources.description;
        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        private bool running = false;
        public List<Profile_Component> DefaultProfile() {

            return dispatcher.JsonToComponents(Resources.defProfile);
        }
        public LedEffect DefaultLED() {

            return dispatcher.JsonToLED(Resources.defProfile);
        }

        public void Exit() {
            udpClient.Close();
            udpClient = null;
            running = false;
            //readThread.Abort();
        }

        public string[] GetInputData() {
            return new string[] {
                "Speed","RPM","Steer","Force_long","Force_lat","Pitch","Roll","Yaw",
                "suspen_pos_bl","suspen_pos_br","suspen_pos_fl","suspen_pos_fr",
                "suspen_vel_bl","suspen_vel_br","suspen_vel_fl","suspen_vel_fr","VelocityX","VelocityY","VelocityZ"
            };
        }

        public void SetReferences(IProfileManager controller,IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init() {
            var pConfig = dispatcher.GetConfigObject<Config>();
            udpClient = new UdpClient(pConfig.Port);
            udpClient.Client.ReceiveTimeout = 2000;
            running = true;
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();

        }

        private void ReadFunction() {
            try {
                while (running) {
                    try
                    {
                        byte[] rawData = udpClient.Receive(ref remoteIP);
                        float speed = BitConverter.ToSingle(rawData, 28);
                        float rpm = BitConverter.ToSingle(rawData, 148) / 30;

                        float VelocityX = (float)(Math.Asin(BitConverter.ToSingle(rawData, 32)) * 57.3);
                        float VelocityY = (float)(Math.Asin(BitConverter.ToSingle(rawData, 36)) * 57.3);
                        float VelocityZ = (float)(Math.Asin(BitConverter.ToSingle(rawData, 40)) * 57.3);



                        float steer = BitConverter.ToSingle(rawData, 120);
                        float g_long = BitConverter.ToSingle(rawData, 140);  // *-5
                        float g_lat = BitConverter.ToSingle(rawData, 136); // *-3
                        float forwardX = BitConverter.ToSingle(rawData, 56);
                        float forwardY = BitConverter.ToSingle(rawData, 60);
                        float forwardZ = BitConverter.ToSingle(rawData, 64);

                        float rollX = BitConverter.ToSingle(rawData, 44);
                        float rollY = BitConverter.ToSingle(rawData, 48);
                        float rollZ = BitConverter.ToSingle(rawData, 52);

                        float susp_pos_bl = BitConverter.ToSingle(rawData, 68);
                        float susp_pos_br = BitConverter.ToSingle(rawData, 72);
                        float susp_pos_fl = BitConverter.ToSingle(rawData, 76);
                        float susp_pos_fr = BitConverter.ToSingle(rawData, 80);
                        float susp_velo_bl = BitConverter.ToSingle(rawData, 84);
                        float susp_velo_br = BitConverter.ToSingle(rawData, 88);
                        float susp_velo_fl = BitConverter.ToSingle(rawData, 92);
                        float susp_velo_fr = BitConverter.ToSingle(rawData, 96);

                        float wheel_speed_rl = BitConverter.ToSingle(rawData, 100);
                        float wheel_speed_rr = BitConverter.ToSingle(rawData, 104);


                        float pitch = (float)(Math.Asin(-forwardY) * 57.3);
                        float roll = -(float)(Math.Asin(-rollY) * 57.3);
                        float yaw = (float)Math.Atan2(forwardY + forwardX, forwardZ) * 57.3f;


                        controller.SetInput(0, speed);
                        controller.SetInput(1, rpm);
                        controller.SetInput(2, steer);

                        controller.SetInput(3, g_long);
                        controller.SetInput(4, g_lat);

                        controller.SetInput(5, pitch);
                        controller.SetInput(6, roll);
                        controller.SetInput(7, yaw);
                        controller.SetInput(8, susp_pos_bl);
                        controller.SetInput(9, susp_pos_br);
                        controller.SetInput(10, susp_pos_fl);
                        controller.SetInput(11, susp_pos_fr);
                        controller.SetInput(12, susp_velo_bl);
                        controller.SetInput(13, susp_velo_br);
                        controller.SetInput(14, susp_velo_fl);
                        controller.SetInput(15, susp_velo_fr);
                        controller.SetInput(16, VelocityX);
                        controller.SetInput(17, VelocityY);
                        controller.SetInput(18, VelocityZ);
                    } catch (Exception ex) { }
                
                }
            
            } catch (ThreadAbortException) { }
        }

      
        public void PatchGame() {
            byte filePatched = 0;
            string[] path = new string[] {
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games/DiRT Rally 2.0/hardwaresettings/hardware_settings_config.xml",
             Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games/DiRT Rally 2.0/hardwaresettings/hardware_settings_config_vr.xml",
        };

            for (int i = 0; i < path.Length; i++) {

                if (File.Exists(path[i])) {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(path[1]);
                    XmlNode root = doc.DocumentElement;
                    XmlNode node = root.SelectSingleNode("motion_platform");
                    node.SelectSingleNode("dbox").Attributes["enabled"].Value = "true";
                    node.SelectSingleNode("udp").Attributes["enabled"].Value = "true";
                    node.SelectSingleNode("udp").Attributes["ip"].Value = "127.0.0.1";
                    node.SelectSingleNode("udp").Attributes["extradata"].Value = "1";
                    node.SelectSingleNode("udp").Attributes["port"].Value = "20777";
                    node.SelectSingleNode("udp").Attributes["delay"].Value = "1";

                    doc.Save(path[i]);
                    filePatched++;
                    dispatcher.ShowNotification(NotificationType.INFO,path[i] + " patched!");
                }
            }

            if(filePatched == 0) {
                dispatcher.DialogShow("Could not patch dirt rally 2. Make sure to start the game at least once before patching!",DIALOG_TYPE.INFO);
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
