using FlatBuffers;
using KartKraft;
using KartKraftPlugin;
using KartKraftPlugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Kart Kraft")]
    [ExportMetadata("Version", "1.0")]
    public class KartKraftPlugin : Game
    {

        public string PROCESS_NAME => "project_k";

        private UdpClient udpClient;
        public Thread ReadThread;
        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 5000);
        private PropertyInfo[] inputs;

        public int STEAM_ID => 406350;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => false;

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        public IMainFormDispatcher dispatcher { get; private set; }

        private IProfileManager controller;
        private volatile bool running = false;

        public LedEffect DefaultLED()
        {
            return new LedEffect(

               EFFECT_TYPE.KNIGHT_RIDER,
               2,
               new YawColor[] {
                    new YawColor(255, 255, 255),
                    new YawColor(80, 80, 80),
                    new YawColor(255, 255, 0),
                    new YawColor(0, 0, 255),
               },
               1.1f);
        }
        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit()
        {
            if (ReadThread != null)
            {
                running = false;
               // ReadThread.Abort();
                udpClient.Close();
                udpClient = null;
            }
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
        public void Init()
        {
            try
            {
                var pConfig = dispatcher.GetConfigObject<Config>();
                udpClient = new UdpClient(pConfig.Port);
                udpClient.Client.ReceiveTimeout = 5000;
                running = true;
                ReadThread = new Thread(new ThreadStart(ReadFunction));
                ReadThread.Start();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            
        }

        public string[] GetInputData()
        {
            
            Type t = typeof(Motion);
            PropertyInfo[] fields = t.GetProperties();

            List<PropertyInfo> list = new List<PropertyInfo>(); 
            
            foreach (var item in fields)
            {
               if( item.PropertyType == typeof(float))
                {
                    list.Add(item);
                }
            }

            inputs = list.ToArray();
            return inputs.Select(x => x.Name).ToArray();
        }

        public void PatchGame()
        {
            return;
        }
        private void ReadFunction()
        {
            while (running)
            {
                try
                {
                    byte[] array = udpClient.Receive(ref RemoteIpEndPoint);
                    ByteBuffer by = new ByteBuffer(array);

                    if (KartKraft.Frame.FrameBufferHasIdentifier(by))
                    {
                        KartKraft.Frame frame = KartKraft.Frame.GetRootAsFrame(by);


                        //Console.WriteLine(array.Length);
                        if (frame.Motion.HasValue)
                        {

                            PropertyInfo[] fields = typeof(Motion).GetProperties();
                            for (int i = 0; i < inputs.Length; i++)
                            {
                                controller.SetInput(i, (float)inputs[i].GetValue(frame.Motion.Value));

                            }
                        }


                    }
                } catch(SocketException) { }

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
