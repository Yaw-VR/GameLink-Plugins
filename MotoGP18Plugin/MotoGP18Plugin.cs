using MotoGP18Plugin.Properties;
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
namespace MotoGP18Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "MotoGP 18")]
    [ExportMetadata("Version", "1.1")]
    class MotoGP18Plugin : Game
    {
       

        private IPEndPoint senderIP = new IPEndPoint(IPAddress.Any, 0);

        public string PROCESS_NAME => "motogp18";
        public string AUTHOR => "YawVR";
        public int STEAM_ID => 775900;
        public bool PATCH_AVAILABLE => false;

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");



        UdpClient udpClient;
        Thread readThread;



        public int port = 7100;
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
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit() {

            udpClient.Close();
            udpClient = null;
            running = false;
            //readThread.Abort();
        }
        public void PatchGame()
        {
            return;
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
            udpClient = new UdpClient(pConfig.Port);
            udpClient.Client.ReceiveTimeout = 5000;
            running = true;
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }

        private void ReadFunction() {

            Packets sende = new Packets();
            FieldInfo[] fields = typeof(Packets).GetFields();
        

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
                            Console.WriteLine(i);
                            controller.SetInput(i, (float)fields[i].GetValue(sende));
                        }
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e);

                    }
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


