using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using WarthunderPlugin.Properties;
using YawGLAPI;

namespace WarthunderPlugin
{

    [Export(typeof(Game))]
    [ExportMetadata("Name", "War Thunder")]
    [ExportMetadata("Version", "1.2")]
    class WarthunderPlugin : Game {
  

        Thread readThread;

        private volatile bool running = false;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        public int STEAM_ID => 236390;
        public string PROCESS_NAME => "aces";
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "YawVR";

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        public void Exit() {
            //readThread.Abort();
            running = false;

        }

        public void Init() {
            running = true;
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }

        private async void ReadFunction() {
            
            Debug.WriteLine("WARTHUNDER READ STARTED");
            try {
                HttpClient client = new HttpClient();
                while (running) {
               
                    string content = await client.GetStringAsync("http://127.0.0.1:8111/indicators");
                  
                       //  var serializer = new DataContractJsonSerializer(typeof(Structure));
                //    using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(content))) {
                       
                          var thunderData = JsonConvert.DeserializeObject<Structure>(content);


                            float pitch = thunderData.aviahorizon_pitch;
                            float roll = thunderData.aviahorizon_roll;


                            float rpm = thunderData.rpm;
                            float throttle = thunderData.throttle;
                            float compass = thunderData.compass;

                      

                            //controller.SetOrientation(0, thunderData.aviahorizon_pitch, -thunderData.turn * 10);

                            controller.SetInput(0, pitch);
                            controller.SetInput(1, roll);
                            controller.SetInput(2, rpm);
                            controller.SetInput(3, throttle);
                            controller.SetInput(4, compass);

                            controller.SetInput(5, thunderData.weapon1);
                            controller.SetInput(6, thunderData.stick_elevator);
                            controller.SetInput(7, thunderData.stick_ailerons);
                            controller.SetInput(8, thunderData.speed);
                            controller.SetInput(9, thunderData.prop_pitch);
                            controller.SetInput(10, thunderData.bank);
                            controller.SetInput(11, thunderData.turn);

                            Thread.Sleep(20);
                        
                      
                    }

                
               
            }
            catch(Exception) {
                Thread.Sleep(1000);
            }
          

            Console.WriteLine("WARTHUNDER READ THREAD ENDED");
        }

        public string[] GetInputData() {
            return new string[] {
                "AVIA_PITCH","ROLL","RPM","THROTTLE","COMPASS","WEAPON1","STICK_ELEVATOR","STICK_AILERONS","SPEED","PROP_PITCH","BANK","TURN"
            };
        }

        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }
        public LedEffect DefaultLED() {
            return new LedEffect(
              
               EFFECT_TYPE.FLOW_LEFTRIGHT,
               1,
               new YawColor[] {
                    new YawColor(128, 210, 242),
                    new YawColor(80,80,80),
                    new YawColor(252, 139, 167),
                    new YawColor(128, 210, 242),
               },
               -70f);
        }

     
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
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
            return null;
        }
    }
}
