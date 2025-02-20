using EuroTruckSimulator2Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using YawGLAPI;
namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Euro Truck Simulator 2")]
    [ExportMetadata("Version", "1.0")]
    class EuroTruckSimulator2Plugin : Game {
       
        public string PROCESS_NAME => "eurotrucks2";
        public int STEAM_ID => 227300;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => true;
        Thread readThread;

        public static MemoryMappedFile mmf;
        public static MemoryMappedViewStream mmfvs;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        const string Ets2TelemetryMappedFileName = "Local\\Ets2TelemetryServer";


        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");

        public Stream Background => GetStream("wide.png");

        public string Description => Resources.description;


        private bool running = false;
        static public bool MemOpen() // open the mapped file
        {
            try {
                mmf = MemoryMappedFile.OpenExisting(Ets2TelemetryMappedFileName);
                return true;
            }
            catch {
                return false;
            }

        }
        public LedEffect DefaultLED() {

            return dispatcher.JsonToLED(Resources.defProfile);
        }

        public List<Profile_Component> DefaultProfile() {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }
        public void PatchGame()
        {
            string path =  $"{dispatcher.GetInstallPath("Euro Truck Simulator 2")}/bin";

            using (WebClient wc = new WebClient())
            {
                string tempFile = Path.GetTempFileName();
                Debug.WriteLine("Downloading ETSMod");
                wc.DownloadFile("https://yaw.one/gameengine/Plugins/Euro_Truck_Simulator_2/etsplugin.zip", tempFile);
                Debug.WriteLine($"Downloaded to {tempFile}");
                dispatcher.ExtractToDirectory(tempFile, path, true);

            }


        }
        public void Exit() {
            try
            {
                mmf.Dispose();
                running = false;
            } catch { }
        }

        public string[] GetInputData() {

            Type t = typeof(Ets2TelemetryStructure);
            FieldInfo[] fields = t.GetFields();

            List<string> inputs = new List<string>();

            for (int i = 0; i < fields.Length; i++) {
              inputs.Add(fields[i].Name);
            }
            return inputs.ToArray();
        }

     
        public void SetReferences(IProfileManager controller,IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init() {    
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }

        private void ReadFunction() {

           
            FieldInfo[] fields = typeof(Ets2TelemetryStructure).GetFields();
            Ets2TelemetryStructure createdObject = new Ets2TelemetryStructure();
            running = true;
            if (MemOpen()) {
                while (running) {


                    using (var accessor = mmf.CreateViewAccessor()) {
                        byte[] rawData = new byte[Marshal.SizeOf(typeof(Ets2TelemetryStructure))];
                        accessor.ReadArray(0, rawData, 0, rawData.Length);
                        IntPtr reservedMemPtr = IntPtr.Zero;
                        try {
                            reservedMemPtr = Marshal.AllocHGlobal(rawData.Length);
                            Marshal.Copy(rawData, 0, reservedMemPtr, rawData.Length);
                            createdObject = (Ets2TelemetryStructure)Marshal.PtrToStructure(reservedMemPtr, typeof(Ets2TelemetryStructure));
                            createdObject.rotationX *= 360;
                            createdObject.rotationY *= 360;
                            createdObject.rotationZ *= 360;
                        }
                        finally {
                            if (reservedMemPtr != IntPtr.Zero)
                                Marshal.FreeHGlobal(reservedMemPtr);
                        }

                    }


                      for (int i = 0; i < fields.Length; i++) {
                          //Console.WriteLine(i);
                          //Console.WriteLine(fields[i].FieldType);

                          if (fields[i].FieldType == typeof(float) ||
                              fields[i].FieldType == typeof(int) ||
                              fields[i].FieldType == typeof(byte)) controller.SetInput(i, Convert.ToSingle((fields[i].GetValue(createdObject))));
                      }
                    Thread.Sleep(20);
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
            return null;
        }
    }
    

}


