
using RedRoverPlugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using YawGLAPI;

namespace RedRoverPlugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Red Rover")]
    [ExportMetadata("Version", "1.0")]
    class RedRoverPlugin : Game {

        private static string FLOAT_REGEX = @"-?[0-9]+(?:\.[0-9]+)?";
        private static string regex = @"Roll:("+FLOAT_REGEX+") Pitch:("+FLOAT_REGEX+") Yaw:("+FLOAT_REGEX+") VeloX:("+FLOAT_REGEX+") VeloY:("+FLOAT_REGEX+") VeloZ:("+FLOAT_REGEX+") RPM:("+FLOAT_REGEX+") Gear:([0-9]+|R) Alti:("+FLOAT_REGEX+") Jets:("+FLOAT_REGEX+")";
        private UdpClient udpClient;
        private Thread readThread;
        private IPEndPoint remoteIP;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        private volatile bool running = false;
        public string PROCESS_NAME => "RedRover";
        public int STEAM_ID => 819060;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => false;

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        public LedEffect DefaultLED() {
            return new LedEffect(

               EFFECT_TYPE.COLORCHANGE_LEFTRIGHT,
               0,
               new YawColor[] {
                    new YawColor(190, 250, 192),
                    new YawColor(80,80,80),
                    new YawColor(80,80,80),
                    new YawColor(255, 105, 36),
                },
               -1f);
        }

        public List<Profile_Component> DefaultProfile() {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit() {
            udpClient.Close();
            udpClient = null;
            running = false;
           // readThread.Abort();
        }

        public string[] GetInputData() {
            return new string[] {
                "Roll","Pitch","Yaw","VeloX","VeloY","VeloZ","RPM","Gear","Alti","Jets"
            };
        }
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

        public string GetDescription()
        {
            return Resources.description;
        }
        public void Init() {
            udpClient = new UdpClient(3001);
            udpClient.Client.ReceiveTimeout = 5000;
            running = true;
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }

        private void ReadFunction() {
            try {
                float[] data = new float[10];
                while (running) {
                    try
                    {
                        byte[] rawData = udpClient.Receive(ref remoteIP);

                        string input = Encoding.ASCII.GetString(rawData);
                        Match m = Regex.Match(input, regex);

                        if (m.Success && m.Groups.Count > 10)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                float.TryParse(m.Groups[i + 1].Value, out data[i]);

                                controller.SetInput(i, data[i]);
                            }
                        }
                    } catch(SocketException) { }

                  
                }
            }
            catch (SocketException) {
            }
            catch (ThreadAbortException) { }
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
            return null;
        }
    }
}
