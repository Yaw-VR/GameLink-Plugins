using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using WarplanesWW1Plugin.Properties;
using YawGLAPI;
namespace WarplanesWW1Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name","Warplanes WW1")]
    [ExportMetadata("Version","1.0")]
    public class WarplanesWW1Plugin : Game
    {
       

        private MemoryMappedFile mmf;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private volatile bool running = false;
        private Thread readThread;

        public int STEAM_ID => 1546500;

        public string PROCESS_NAME => "WW1";

        public bool PATCH_AVAILABLE => false;

        public string AUTHOR => "YawVR";

        public Stream Logo => GetStream("logo.png");

        public Stream SmallLogo => GetStream("recent.png");

        public Stream Background => GetStream("wide.png");

        public string Description => String.Empty;

        public LedEffect DefaultLED()
        {
            return new LedEffect()
            {
                EffectID = EFFECT_TYPE.KNIGHT_RIDER,
                InputID = 2,
                Colors = new System.Collections.ObjectModel.ObservableCollection<YawColor>
                {
                    new YawColor(0,255,0),
                    new YawColor(0,0,255),
                    new YawColor(0,255,255),
                    new YawColor(255,0,0),
                }
            };
        }

        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit()
        {
            running = false;
            mmf = null;
        }

        public string[] GetInputData()
        {
            Type t = typeof(Packet);
            FieldInfo[] fields = t.GetFields();

            string[] inputs = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                inputs[i] = fields[i].Name;
            }
            return inputs;
        }

        public void Init()
        {
            running = true;
            readThread = new Thread(new ThreadStart(ReadThread));
            readThread.Start();
        }
        private void ReadThread()
        {
            Type t = typeof(Packet);
            FieldInfo[] fields = t.GetFields();

            while (!OpenMMF() && running)
            {
                dispatcher.ShowNotification(NotificationType.WARNING, "Cant reach MMF, trying again..");
                Console.WriteLine("Cant connect to MMF, trying again..");
                Thread.Sleep(4000);

            }

            while (running)
            {
                Packet p = ReadPacket();



                for (int i = 0; i < fields.Length; i++)
                {
                    controller.SetInput(i, (float)fields[i].GetValue(p));
                }

                Thread.Sleep(20);
            }
        }

        private Packet ReadPacket()
        {
            using (var stream = mmf.CreateViewStream())
            {
                using (var reader = new BinaryReader(stream))
                {
                    var size = Marshal.SizeOf(typeof(Packet));
                    var bytes = reader.ReadBytes(size);
                    var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    var data = (Packet)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Packet));
                    handle.Free();
                    return data;
                }
            }
        }

        private bool OpenMMF()
        {
            try
            {
                mmf = MemoryMappedFile.OpenExisting("ww1");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                dispatcher.DialogShow("Admin privileges are needed to read Redout's MMF. Restart GameEngine in admin mode?", DIALOG_TYPE.QUESTION, delegate {

                    dispatcher.RestartApp(true);
                });


                dispatcher.ExitGame();
                return false;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        public void PatchGame()
        {
            return;
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
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
