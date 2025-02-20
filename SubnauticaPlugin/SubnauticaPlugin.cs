using SubnauticaPlugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Reflection;
using System.Threading;
using YawGLAPI;
namespace SubnauticaPlugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Subnautica")]
    [ExportMetadata("Version", "1.0")]
    class SubnauticaPlugin : Game
    {

        private struct DataStructure
        {
            public float InVehicle,Heading,AVelocityX, AVelocityY, AVelocityZ,VelocityX,VelocityY,VelocityZ,ForwardForce,BackwardForce, SidewardForce;
        }

        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;

        private Thread readThread;
        private bool running = false;




        public string PROCESS_NAME => "Subnautica";
        public int STEAM_ID => 264710;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => true;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");
        public string Description => Resources.Description;
        //https://yaw.one/gameengine/Plugins/SubnauticaPlugin/SNTelemetry.zip

        public void PatchGame()
        {
            /* TODO - INSTALL QMOD */

            var installPath = dispatcher.GetInstallPath("subnautica");
            Debug.WriteLine($"Install path: {installPath}");
            if(File.Exists($"{installPath}/Subnautica.exe"))
            {
                using(WebClient wc = new WebClient())
                {
                    string tempFile = Path.GetTempFileName();
                    Debug.WriteLine("Downloading SNMod");
                    wc.DownloadFile("https://yaw.one/gameengine/Plugins/SubnauticaPlugin/SNTelemetry.zip", tempFile);
                    Debug.WriteLine($"Downloaded to {tempFile}");
                    dispatcher.ExtractToDirectory(tempFile,installPath,true);

                }

            } else
            {
                dispatcher.DialogShow("Could not find Subnautica", DIALOG_TYPE.INFO);
            }

            Debug.WriteLine("Subnautica patched");
        }

        public void Exit()
        {
            running = false;
        }
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init()
        {

            running = true;
            readThread = new Thread(ReadFunction);
            readThread.Start();

            
        }
        private void ReadFunction()
        {
            try
            {

                DataStructure data = new DataStructure();
                FieldInfo[] fields = typeof(DataStructure).GetFields();

                
                while (running)
                {
                    try
                    {
                        using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("yawmmfsn"))
                        {
                      
                            using (var accessor = mmf.CreateViewAccessor())
                            {
                                accessor.Read<DataStructure>(0, out data);
                            }

                            for (int i = 0; i < fields.Length; i++)
                            {
                                controller.SetInput(i, (float)fields[i].GetValue(data));
                            }
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        Debug.WriteLine("Memory-mapped file does not exist. Run Process A first, then B.");
                        Thread.Sleep(3000);
                    }
                    Thread.Sleep(20);


                }
            }
            catch (ThreadAbortException)
            {


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        public string[] GetInputData()
        {

            Type t = typeof(DataStructure);
            FieldInfo[] fields = t.GetFields();

            string[] inputs = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                inputs[i] = fields[i].Name;
            }
            return inputs;
        }
        public LedEffect DefaultLED()
        {

            return dispatcher.JsonToLED(Resources.Profile);
        }
        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(Resources.Profile);
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
