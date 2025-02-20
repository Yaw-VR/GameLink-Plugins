using Dirt4Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Xml;
using YawGLAPI;

namespace Dirt4Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Dirt Rally 4")]
    [ExportMetadata("Version", "1.0")]
    class Dirt4Plugin : Game {
        
        UdpClient udpClient;


        Thread readThread;
        private IPEndPoint remoteIP;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private bool running = false;
        public string PROCESS_NAME => "dirt4";
        public int STEAM_ID => 421020;
        public bool PATCH_AVAILABLE => true;
        public string AUTHOR => "YawVR";

        public string Description => Resources.description;
        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        //private const int Port = 20777;
        
        public List<Profile_Component> DefaultProfile() {

            return new List<Profile_Component>() {
                 new Profile_Component(7,0,1f,1f,0f,false,true,-1,1f),

                new Profile_Component(1,3, 2,2,0f,false,false,-1,1f), //amp
                new Profile_Component(0,4, 17, 17,0f,true,false,-1,1f), //hz

                new Profile_Component(5,1,0.4f,0.4f,0f,false,false,-1,1f), //pitch_orien
                new Profile_Component(6,2,1,1,0f,false,false,-1,1f), //roll_orien

                new Profile_Component(3,1, 5,5,0f,false,true,-1,1f), //pitch_g
                new Profile_Component(4,2,3,3,0f,false,true,-1,1f), //roll_g
            };
        }
        public LedEffect DefaultLED() {

            return new LedEffect(

                EFFECT_TYPE.KNIGHT_RIDER,
                1,
                new YawColor[] {
                    new YawColor(255, 255, 255),
                    new YawColor(80, 80, 80),
                    new YawColor(255, 255, 0),
                    new YawColor(0, 0, 255),
                },
                1f);
        }

        public void Exit() {
            udpClient.Close();
            udpClient = null;
            running = false;
        }

        public string[] GetInputData() {
            return new string[] {
                "Speed","RPM","Steer","Force_long","Force_lat","Pitch","Roll","Yaw",
                "suspen_pos_bl","suspen_pos_br","suspen_pos_fl","suspen_pos_fr",
                "suspen_vel_bl","suspen_vel_br","suspen_vel_fl","suspen_vel_fr","VelocityX","VelocityY","VelocityZ"
            };
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init() {

            var pConfig = dispatcher.GetConfigObject<Config>();
            dispatcher.DialogShow($"Using port {pConfig.Port}",DIALOG_TYPE.INFO);
            udpClient = new UdpClient(pConfig.Port);
            readThread = new Thread(new ThreadStart(ReadFunction));
            running = true;
            readThread.Start();

        }
       
        private void ReadFunction() {
            try {
                while (running) {
                    byte[] rawData = udpClient.Receive(ref remoteIP);
                    float speed = ReadSingle(rawData, 28, true);
                    float rpm = ReadSingle(rawData, 148, true) / 30;

                    float VelocityX = (float)(ReadSingle(rawData, 32, true));
                    float VelocityY = (float)(ReadSingle(rawData, 36, true));
                    float VelocityZ = (float)(ReadSingle(rawData, 40, true));



                    float steer = ReadSingle(rawData, 120, true);
                    float g_long = ReadSingle(rawData, 140, true);  // *-5
                    float g_lat = ReadSingle(rawData, 136, true); // *-3
                    float forwardX = ReadSingle(rawData, 56, true);
                    float forwardY = (float)(ReadSingle(rawData, 60, true));
                    float forwardZ = (float)(ReadSingle(rawData, 64, true));

                    float rollX = ReadSingle(rawData, 44, true);
                    float rollY = (float)(ReadSingle(rawData, 48, true));
                    float rollZ = (float)(ReadSingle(rawData, 52, true));

                    float susp_pos_bl = (float)ReadSingle(rawData, 68, true);
                    float susp_pos_br = (float)ReadSingle(rawData, 72, true);
                    float susp_pos_fl = (float)ReadSingle(rawData, 76, true);
                    float susp_pos_fr = (float)ReadSingle(rawData, 80, true);
                    float susp_velo_bl = (float)ReadSingle(rawData, 84, true);
                    float susp_velo_br = (float)ReadSingle(rawData, 88, true);
                    float susp_velo_fl = (float)ReadSingle(rawData, 92, true);
                    float susp_velo_fr = (float)ReadSingle(rawData, 96, true);

                    float wheel_speed_rl = (float)ReadSingle(rawData, 100, true);
                    float wheel_speed_rr = (float)ReadSingle(rawData, 104, true);


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

                }

            }
            catch (SocketException) {
            }
            catch (ThreadAbortException) { }
        }

      
        public void PatchGame() {
            byte filePatched = 0;
            string[] path = new string[] {
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games/DiRT 4/hardwaresettings/hardware_settings_config.xml",
             Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games/DiRT 4/hardwaresettings/hardware_settings_config_vr.xml",
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

            if (filePatched == 0) {
                dispatcher.DialogShow("Could not patch dirt rally 4. Make sure to start the game at least once before patching!",DIALOG_TYPE.INFO);
            }

        }

        float ReadSingle(byte[] data, int offset, bool littleEndian)
        {
            if (BitConverter.IsLittleEndian != littleEndian)
            {   // other-endian; reverse this portion of the data (4 bytes)
                byte tmp = data[offset];
                data[offset] = data[offset + 3];
                data[offset + 3] = tmp;
                tmp = data[offset + 1];
                data[offset + 1] = data[offset + 2];
                data[offset + 2] = tmp;
            }
            return BitConverter.ToSingle(data, offset);
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
