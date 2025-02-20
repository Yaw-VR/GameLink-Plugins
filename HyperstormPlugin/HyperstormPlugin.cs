using HyperstormPlugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using YawGLAPI;

namespace HyperstormPlugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Hyperstorm")]
    [ExportMetadata("Version", "1.0")]
    class HyperstormPlugin : Game {
        
        private UdpClient udpClient;
        private Thread readThread;
        private IPEndPoint remoteIP;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        private volatile bool running = false;
        public string PROCESS_NAME => "HyperStorm";
        public int STEAM_ID => 1147840;
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "YawVR";

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        public LedEffect DefaultLED() {
            return new LedEffect(
               
               EFFECT_TYPE.COLORCHANGE_LEFTRIGHT,
               2,
               new YawColor[] {
                    new YawColor(190, 250, 192),
                    new YawColor(80,80,80),
                    new YawColor(80,80,80),
                    new YawColor(255, 105, 36),
                },
               -1f);
        }

        public List<Profile_Component> DefaultProfile() {
            return new List<Profile_Component>() {
                new Profile_Component(0,0, 1,1,0f,false,false,-1,1f),
                new Profile_Component(1,1, 1,1,0f,false,true,-1,1f),
                new Profile_Component(2,2, 1,1,0f,false,false,-1,1f)
            };
        }

        public void Exit() {
            udpClient.Close();
            udpClient = null;
            running = false;
           // readThread.Abort();
        }

        public string[] GetInputData() {
            return new string[] {
                "Yaw","Pitch","Roll","Sway","Heave","Surge","Wind"
            };
        }

      
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
        public void Init() {

            var pConfig = dispatcher.GetConfigObject<Config>();
            udpClient = new UdpClient(pConfig.Port);
            udpClient.Client.ReceiveTimeout = 5000;
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

                        float yaw = BitConverter.ToSingle(rawData, 4);
                        float pitch = BitConverter.ToSingle(rawData, 0);
                        float roll = BitConverter.ToSingle(rawData, 8);


                        float sway = BitConverter.ToSingle(rawData, 12);
                        float heave = BitConverter.ToSingle(rawData, 16);
                        float surge = BitConverter.ToSingle(rawData, 20);

                        float wind = BitConverter.ToSingle(rawData, 24);

                        controller.SetInput(0, yaw);
                        controller.SetInput(1, pitch);
                        controller.SetInput(2, roll);

                        controller.SetInput(3, sway);
                        controller.SetInput(4, heave);
                        controller.SetInput(5, surge);



                        controller.SetInput(6, wind);
                    }
                    catch (SocketException) { }
                }
            }
            catch (SocketException) {
            } catch(ThreadAbortException) { }
        }

        public void PatchGame()
        {

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
