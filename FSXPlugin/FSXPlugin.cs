using FSXPlugin.Properties;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Microsoft Flight Simulator X")]
    [ExportMetadata("Version", "1.0")]
    class FSXPlugin : Game {

        // SimConnect object
     

        enum DEFINITIONS {
            Struct1,
        }

        enum DATA_REQUESTS {
            REQUEST_1,
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct Struct1 {
            // this is how you declare a fixed size string
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String title;
            public double latitude;
            public double longitude;
            public double altitude;
            public double pitch;
            public double roll;
            public double heading;
            public double vel_x;
            public double vel_y;
            public double vel_z;
            public double velocity;
            public double engine_vibration;

        };

        // User-defined win32 event
        const int WM_USER_SIMCONNECT = 0x0402;
        public string PROCESS_NAME => "fsx";
        public int STEAM_ID => 314160;
        public bool PATCH_AVAILABLE => true;
        public string AUTHOR => "YawVR";

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        private Thread readThread;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        private volatile bool running = false;
        public IntPtr Handle { get; private set; }

        public List<Profile_Component> DefaultProfile() {

            return new List<Profile_Component>() {
                new Profile_Component(0,0, 1f,1f,0f,false,true,-1,1f),

                new Profile_Component(1,1, 1f,1f,0f,false,false,-1,1f), //amp
                new Profile_Component(2,2, 1f,1f,0f,false,false,-1,1f), //hz
            };
        }
        public LedEffect DefaultLED() {

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

        public void Exit() {
            running = false;
            
        }

        public string[] GetInputData() {
            return new string[] {
                "Yaw","Pitch","Roll","Altitude","Angular_Velocity_X","Angular_Velocity_Y","Angular_Velocity_Z","Velocity","Engine_RPM"
            };
        }
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
        public void Init() {
               try {
                
                   SimConnect simconnect = null;
                   simconnect = new SimConnect("Managed Data Request", this.Handle, WM_USER_SIMCONNECT, null, 0);
                   // listen to connect and quit msgs
                 //  simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(simconnect_OnRecvOpen);
                 //  simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(simconnect_OnRecvQuit);

                   // listen to exceptions
                   //simconnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(simconnect_OnRecvException);

                   // define a data structure
                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Title", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Plane Latitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Plane Longitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Plane Altitude", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE HEADING DEGREES TRUE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY X", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY Y", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY Z", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GROUND VELOCITY", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                   simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG RPM", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                   // IMPORTANT: register it with the simconnect managed wrapper marshaller
                   // if you skip this step, you will only receive a uint in the .dwData field.
                   simconnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);

                   // catch a simobject data request
                   simconnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(simconnect_OnRecvSimobjectDataBytype);


                running = true;
                   readThread = new Thread(new ParameterizedThreadStart(ReadFunction));
                   readThread.Start(simconnect);
               }
               catch (COMException ex) {
                    dispatcher.ExitGame();
               }
               catch(FileNotFoundException) {
                    dispatcher.ExitGame();
                }
               
        }

           private void simconnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data) {

               switch ((DATA_REQUESTS)data.dwRequestID) {
                   case DATA_REQUESTS.REQUEST_1:
                   Struct1 s1 = (Struct1)data.dwData[0];
                   controller.SetInput(0, (float)s1.heading);
                   controller.SetInput(1, (float)s1.pitch);
                   controller.SetInput(2, (float)s1.roll);

                   controller.SetInput(3, (float)s1.altitude);

                   controller.SetInput(4, (float)s1.vel_x);
                   controller.SetInput(5, (float)s1.vel_y);
                   controller.SetInput(6, (float)s1.vel_z);
                   controller.SetInput(7, (float)s1.velocity);
                   controller.SetInput(8, (float)s1.engine_vibration);

                   break;

                   default:
                   Console.WriteLine("Unknown request ID: " + data.dwRequestID);
                   break;
               }
           }

           private void ReadFunction(object simconnectObj) {
               try {
                SimConnect simconnect = (SimConnect)simconnectObj;
                   Console.WriteLine("FSX read started");
                   while (running) {
                       // The following call returns identical information to:
                       // simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_1, DEFINITIONS.Struct1, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE);

                       simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_1, DEFINITIONS.Struct1, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                       simconnect.ReceiveMessage();
                       Thread.Sleep(20);
                   }

               }
               catch(COMException) {
                dispatcher.ExitGame();
               }
               catch (ThreadAbortException) { }
           }

        public void PatchGame() {
            string tempFile = Path.GetTempFileName() + ".msi";
            
            using(WebClient wc = new WebClient()) {
                wc.DownloadFile("http://yaw.one/gameengine/Plugins/Microsoft_Flight_Simulator_X/SimConnect.msi", tempFile);
            }

            Process p = Process.Start(tempFile);
            p.WaitForExit();
            
            dispatcher.RestartApp(false);

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
