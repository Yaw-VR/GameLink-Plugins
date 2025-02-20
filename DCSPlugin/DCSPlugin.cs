using DCSPlugin;
using DCSPlugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "DCS")]
    [ExportMetadata("Version", "1.6")]
    class DCSPlugin : Game {
        UdpClient udpClient;

        private Thread readThread;

        private bool stop = false;

        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 41230);

        public int STEAM_ID => 223750;
        public string PROCESS_NAME => "DCS"; // No need to wait for process
        public bool PATCH_AVAILABLE => true;
        public string AUTHOR => "YawVR";


        public string Description => Resources.description;
        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        private float prevSpeed = 0f;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;

        public LedEffect DefaultLED() {
            return new LedEffect(
          
           EFFECT_TYPE.KNIGHT_RIDER,
           2,
           new YawColor[] {
                new YawColor(66, 135, 245),
                 new YawColor(80,80,80),
                new YawColor(128, 3, 117),
                new YawColor(110, 201, 12),
                },
           0.7f);
        }

        public List<Profile_Component> DefaultProfile() {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit() {
            udpClient.Close();
            udpClient = null;
            stop = true;            
        }

        public string[] GetInputData() {
            return new string[] {
                "YAW","PITCH","ROLL","Indicated_Speed","Acceleration","Rpm","Angular_Roll","Angular_Yaw","Angular_Pitch","Acc_X","Acc_Y","Acc_Z","OnGround","LeftGear","RightGear","NoseGear","TrueAirspeed"
            };
        }

     
        public void SetReferences(IProfileManager controller,IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init() {

            Console.WriteLine("DCS INIT");
            stop = false;
            var config = dispatcher.GetConfigObject<Config>();
            udpClient = new UdpClient(config.Port);
            readThread = new Thread(new ThreadStart(ReadFunction));

            readThread.Start();

        }

        private void ReadFunction() {
            try {
                while (!stop) {
                    byte[] rawData = udpClient.Receive(ref remote);
                    //time, yaw, pitch, roll, angularVelocity.x, angularVelocity.y, angularVelocity.z, acceleration.x, acceleration.y, acceleration.z, ias,rpm, flags


                 
                    string[] data = Encoding.ASCII.GetString(rawData).Split(';');

                    float rawYaw,rawPitch,rawRoll,rawAngularX,rawAngularY,rawAngularZ,rawAccX,rawAccY,rawAccZ,rawSpeed,trueSpeed,rawRpm,rawOnground,rawLeftGear,rawRightGear,rawNoseGear;
                    rawYaw = rawPitch = rawRoll = rawAngularX = rawAngularY = rawAngularZ = rawAccX = rawAccY = rawAccZ = rawSpeed = rawRpm = rawOnground = rawLeftGear = rawRightGear = trueSpeed = rawNoseGear = 0;
                    if (data.Length >= 17)
                    {
                        float.TryParse(data[1], out rawYaw);
                        float.TryParse(data[2], out rawPitch);
                        float.TryParse(data[3], out rawRoll);

                        float.TryParse(data[4], out rawAngularX);
                        float.TryParse(data[5], out rawAngularY);
                        float.TryParse(data[6], out rawAngularZ);

                        float.TryParse(data[7], out rawAccX);
                        float.TryParse(data[8], out rawAccY);
                        float.TryParse(data[9], out rawAccZ);

                        float.TryParse(data[10], out rawSpeed);
                        float.TryParse(data[11], out rawRpm);
                        float.TryParse(data[12], out rawOnground);
                        float.TryParse(data[13], out rawLeftGear);
                        float.TryParse(data[14], out rawRightGear);
                        float.TryParse(data[15], out rawAngularX);
                        float.TryParse(data[16], out trueSpeed);
                    }

                    float yaw   = rawYaw * (180f/(float)Math.PI);
                    float pitch = (rawPitch) * -180f / (float)Math.PI;
                    float roll  = (rawRoll) * -180f / (float)Math.PI;

                    float angularX = rawAngularX * -180f / (float)Math.PI;
                    float angularY = rawAngularY * -180f / (float)Math.PI;
                    float angularZ = rawAngularZ * -180f / (float)Math.PI;

                    //float accX = float.Parse(data[7]);
                    //float accY = float.Parse(data[8]);
                    //float accZ = float.Parse(data[9]);

                    //float speed = float.Parse(data[10]);

                    //float rpm = float.Parse(data[11]);
                    //float onground = float.Parse(data[12]);

                    //float leftGear = float.Parse(data[13]);
                    //float rightGear = float.Parse(data[14]);
                    //float noseGear = float.Parse(data[15]);

                    float acceleration = rawSpeed - prevSpeed;

                    prevSpeed = rawSpeed;

                    //"YAW","PITCH","ROLL","Speed","Acceleration","Rpm","Angular_Roll","Angular_Yaw","Angular_Pitch","Acc_X","Acc_Y","Acc_Z","FLAGS"
                    controller.SetInput(0, yaw);
                    controller.SetInput(1, pitch);
                    controller.SetInput(2, roll);

                    controller.SetInput(3, rawSpeed);
                    controller.SetInput(4, acceleration);
                    controller.SetInput(5, rawRpm);

                    controller.SetInput(6, angularX);
                    controller.SetInput(7, angularY);
                    controller.SetInput(8, angularZ);

                    controller.SetInput(9, rawAccX);
                    controller.SetInput(10, rawAccY);
                    controller.SetInput(11, rawAccZ);

                    controller.SetInput(12, rawOnground);
                    controller.SetInput(13, rawLeftGear);
                    controller.SetInput(14, rawRightGear);
                    controller.SetInput(15, rawNoseGear);
                    controller.SetInput(16, trueSpeed);
                }
            }
            catch(SocketException) { }
            catch (ThreadAbortException) { }
        }

        public void PatchGame() {

                using (WebClient wc = new WebClient()) {
                    string luaContent = wc.DownloadString("http://yaw.one/gameengine/Plugins/DCS/Export.lua");
                    string sgPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "Saved Games/DCS/Scripts");
                    
                    if (!Directory.Exists(sgPath)) {
                    Directory.CreateDirectory(sgPath);
                        //dispatcher.DialogShow("Cant find DCS Scripts folder!\nMake sure to start DCS at least once before patching!",DIALOG_TYPE.INFO);
                        //return;
                    } 
                    StreamWriter sw = new StreamWriter(sgPath + "/Export.lua");
                    sw.WriteLine(luaContent);
                    sw.Close();

                    dispatcher.ShowNotification(NotificationType.INFO,"DCS patched!");
           
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
