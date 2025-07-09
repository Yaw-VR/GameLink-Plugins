using RedOutPlugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using YawGLAPI;

namespace RedOutPlugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Redout")]
    [ExportMetadata("Version", "1.0")]

    public class RedoutPlugin : Game {  

        private MemoryMappedFile mmf;
        private Thread readThread;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        private volatile bool running = false;
        public int STEAM_ID => 517710;
        public string PROCESS_NAME => "redout";
        public string AUTHOR => "YawVR";

        public bool PATCH_AVAILABLE => false;

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        private static readonly string[] inputs = {
            "Yaw","Pitch","Roll","AccX","AccY","AccZ"
        };

        public void Exit() {
            running = false;
           // readThread.Abort();
        }
        public void PatchGame()
        {
            return;
        }
        public string[] GetInputData() {

            return inputs;
        }
        public LedEffect DefaultLED() {
            return dispatcher.JsonToLED(Resources.defProfile);
        }
        public List<Profile_Component> DefaultProfile() {

            return dispatcher.JsonToComponents(Resources.defProfile);
        }
     
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

        public void Init() {
            running = true;
            readThread = new Thread(new ThreadStart(ReadThread));
            readThread.Start();

        }

        private void ReadThread() {
            while (!OpenMMF() && running) {
                dispatcher.ShowNotification(NotificationType.WARNING,"Cant reach MMF, trying again..");
                Console.WriteLine("Cant connect to MMF, trying again..");
                Thread.Sleep(4000);

            }

            while (running) {
                Packet p = ReadPacket();

                Quaternion quater = new Quaternion(p.QuatX,p.QuatY,p.QuatZ,p.QuatW);
              
                float roll = (float)RadianToDegree(quater.toYawFromYUp()) * -1;
                float pitch = (float)RadianToDegree(quater.toPitchFromYUp()) * -1;
                float yaw = (float)RadianToDegree(quater.toRollFromYUp());


                controller.SetInput(0, yaw);
                controller.SetInput(1, pitch);
                controller.SetInput(2, roll);

                controller.SetInput(3, (float)p.AccX);
                controller.SetInput(4, (float)p.AccY);
                controller.SetInput(5, (float)p.AccZ);


                Thread.Sleep(20);
            }
        }

        internal Packet ReadPacket() {
            using (var stream = mmf.CreateViewStream()) {
                using (var reader = new BinaryReader(stream)) {
                    var size = Marshal.SizeOf(typeof(Packet));
                    var bytes = reader.ReadBytes(size);
                    var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    var data = (Packet)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Packet));
                    handle.Free();
                    return data;
                }
            }
        }

        public static double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }
        private bool OpenMMF() {
            try {
                mmf = MemoryMappedFile.OpenExisting("Global\\RedoutSimulatorParams");
                return true;
            }
            catch(UnauthorizedAccessException) {
                dispatcher.DialogShow("Admin privileges are needed to read Redout's MMF. Restart GameEngine in admin mode?", DIALOG_TYPE.QUESTION, delegate {

                    dispatcher.RestartApp(true);
                }); 

                
                dispatcher.ExitGame();
                return false;
            } 
            catch (FileNotFoundException) {
                return false;
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
