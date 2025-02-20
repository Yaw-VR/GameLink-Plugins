using AcesHighPlugin.Properties;
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

namespace AcesHighPlugin
{
   
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Aces High")]
    [ExportMetadata("Version", "1.0")]
    class AcesHighPlugin : Game {
        private bool stopThread;

        private UdpClient udpClient;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private Thread readThread;
        private IPEndPoint remotePoint;

        public int STEAM_ID => 651090;
        public string PROCESS_NAME => string.Empty;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => false;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");
        public string Description => "In Aces High go to Offline/ Options / controls / Force Feedback to enable data output and port configuration(556). ";


        public LedEffect DefaultLED() {
            return new LedEffect(

              EFFECT_TYPE.KNIGHT_RIDER,
              6,
              new YawColor[] {
                new YawColor(66, 135, 245),
                new YawColor(80,80,80),
                new YawColor(128, 3, 117),
                new YawColor(110, 201, 12),
                },
              0.7f);
        }
        public List<Profile_Component> DefaultProfile() {
            return new List<Profile_Component>() {
                new Profile_Component(0,0, 1,1,0f,false,true,-1,1f),
                new Profile_Component(1,1, 1f,1f,0f,false,true,-1,1f),
                new Profile_Component(2,2, 1f,1f,0f,false,true,-1,1f)
            };
        }
        public void Exit() {
            udpClient.Close();
            udpClient = null;
            stopThread = true;
        }

        public string[] GetInputData() {
            return new string[] {
                "Yaw","Pitch","Roll","Yaw_vel","Pitch_vel","Roll_vel","AirSpeed","AirAcceleration","Acc_X","Acc_Y","Acc_Z"
            };
        }

    

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;

        }
        public void Init() {

            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }


        private void ReadFunction() {

            var pConfig = dispatcher.GetConfigObject<Config>();
            udpClient = new UdpClient(pConfig.port);
            Console.WriteLine("aces high udp listening started");
            try {
                while (!stopThread) {
                    {

                        byte[] data = udpClient.Receive(ref remotePoint);

                        string receive = Encoding.ASCII.GetString(data);

                      //  Console.WriteLine(receive);

                        //Version,clock,SimType,x,y,z,xvel,yvel,zvel,xacc,yacc,pacc,roll,pitch,yaw,rollvel,pitchvel,yawvel,rollacc,pitchacc,yawacc
                        float[] rawData = Array.ConvertAll(
                            receive.Split(new[] { ',', }, StringSplitOptions.RemoveEmptyEntries),
                            float.Parse);
                    
                        controller.SetInput(0, rawData[14]);
                        controller.SetInput(1, rawData[13]);
                        controller.SetInput(2, rawData[12]);
                        controller.SetInput(3, rawData[17]);
                        controller.SetInput(4, rawData[16]);
                        controller.SetInput(5, rawData[15]);
                        controller.SetInput(6, (float)Math.Sqrt(Math.Pow(rawData[6], 2) + Math.Pow(rawData[7], 2) + Math.Pow(rawData[8], 2)));

                        controller.SetInput(7, rawData[9]);
                        controller.SetInput(8, rawData[10]);
                        controller.SetInput(9, rawData[11]);

                        //controller.SetInput(3, speed);
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
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
