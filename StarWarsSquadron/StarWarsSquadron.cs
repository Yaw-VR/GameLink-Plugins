using StarWarsSquadron.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using YawGLAPI;
namespace StarWarsSquadron
{


    [Export(typeof(Game))]
    [ExportMetadata("Name", "Star Wars Squadrons")]
    [ExportMetadata("Version", "1.0")]
    class StarWarsSquadron : Game
    {
        [Serializable]
        public class DataFormat {
            public IntPtr[] speed = { (IntPtr)0x0423A270, (IntPtr)0xA0, (IntPtr)0x48, (IntPtr)0x10, (IntPtr)0x230, (IntPtr)0x10, (IntPtr)0x58, (IntPtr)0x28C };
            public IntPtr[] acceleration = { (IntPtr)0x042478B0, (IntPtr)0x18, (IntPtr)0x20, (IntPtr)0x0, (IntPtr)0x40 };
            public IntPtr[] pitch = { (IntPtr)0x03E9FA18, (IntPtr)0x40, (IntPtr)0xC0, (IntPtr)0x88, (IntPtr)0x68, (IntPtr)0x3C0 };
            public IntPtr[] roll = { (IntPtr)0x03E9FA18, (IntPtr)0x40, (IntPtr)0xC0, (IntPtr)0x88, (IntPtr)0x68, (IntPtr)0x3CC };
            public IntPtr[] yaw = { (IntPtr)0x03E9FA18, (IntPtr)0x40, (IntPtr)0xC0, (IntPtr)0x88, (IntPtr)0x68, (IntPtr)0x3C4 };
        }
  

        Thread readThread;
        public string PROCESS_NAME => "starwarssquadrons_launcher";
        public bool PATCH_AVAILABLE => true;
        public string AUTHOR => "YawVR";

        static Process handle;
        static IntPtr Base;
        private DataFormat data = new DataFormat();
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        private volatile bool running = false;
        public int STEAM_ID => 1222730;



        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");



        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        [DllImport("kernel32.dll")]
        public static extern IntPtr ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            [In, Out] byte[] buffer, UInt32 size, out IntPtr lpNumberOfBytesRead);

        public List<Profile_Component> DefaultProfile()
        {
          
            return new List<Profile_Component>() {
                new Profile_Component(0,0, 1f,1f,0f,false,true,-1,1f),
                new Profile_Component(1,1, 31.05f,31.05f,0f,false,false,-1,0.220000029f),
                new Profile_Component(2,2, 12.6f,12.6f,0f,false,true,-1,0.100000024f),

                new Profile_Component(3,1, 0.24f,0.24f,0f,false,true,-1,0.05f),
                
              

            };
        }
        public LedEffect DefaultLED()
        {

            return new LedEffect(

                EFFECT_TYPE.COLORCHANGE_LEFTRIGHT,
                2,
                new YawColor[] {
                    new YawColor(255, 255, 255),
                    new YawColor(80, 80, 80),
                    new YawColor(255, 255, 0),
                    new YawColor(0, 0, 255),
                },
                30f);
        }

        public void Exit()
        {
            running = false;
           // readThread.Abort();
        }

        public string[] GetInputData()
        {
            return new string[] {
                "Yaw","Pitch","Roll","Acceleration"
            };
        }
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

        public void Init()
        {
            data = new DataFormat(); // GetOffset returned null, which is bad, use the hard coded version
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Priority = ThreadPriority.Highest;
            running = true;
            readThread.Start();
        }
        private void ReadFunction()
        {
           
            try
                {
                float yawertek = 0;
                float PitchCalc = 0;
                float rollCalc = 0;
                float yawCalc = 0;
             

                GetBase(PROCESS_NAME, null);
                    while (running)
                    {
                        float Pitch = readPtr(data.pitch, false);
                        float Roll = readPtr(data.roll, false);
                        float Yaw = readPtr(data.yaw, false);
                       // float Speed = readPtr(data.speed, false);
                        float Acceleration = readPtr(data.acceleration, false);
                    //PitchCalc = (Math.Abs(Pitch) < 5f) ? Pitch : PitchCalc;
                        rollCalc = (Math.Abs(Roll) < 5f) ? Roll : rollCalc;
                        yawCalc = (float)Math.Round((Math.Abs(Yaw) < 1f) ? Yaw : yawCalc, 1);
                        yawertek += yawCalc;


                    if (Math.Abs(PitchCalc) - Math.Abs(Pitch) > 0) {
                        
                        PitchCalc = (float)(PitchCalc - 0.02f * Math.Sign(PitchCalc));
                    }
                    else {
                        PitchCalc = Pitch;
                    }
                        
                      // Pitch = Helpers.Lerp(PitchCalc,Pitch,0.008f);                   // PitchCalc = Pitch;

                    controller.SetInput(0, NormalizeAngle((float)yawertek));
                    
                   // if (PitchCalc != 0)
                   // {
                        controller.SetInput(1, PitchCalc);
                  
                    
                    if (rollCalc != 0)
                    {
                        controller.SetInput(2, rollCalc);
                    }

                    
                    controller.SetInput(3, ClampBetween(Acceleration, -100, 100));
                    //controller.SetInput(3, Speed);
                    Thread.Sleep(20);
                    }

                }
                catch (ThreadAbortException) { }
                catch (InvalidOperationException) { }
            
         
        }

        public void PatchGame()
        {
            string name = "Star Wars Squadrons";
            string installPath = dispatcher.GetInstallPath(name);
            Console.WriteLine(installPath);
            if (!Directory.Exists(installPath))

            {
                dispatcher.DialogShow("Can't find Star Wars Squadron install directory? \nOpen Plugin manager?", DIALOG_TYPE.QUESTION, delegate {

                   dispatcher.OpenPluginManager();
                });
                return;
            }
            else
            {
                var fileinfo = new FileInfo(installPath + "\\" + "starwarssquadrons.exe").Length;
                var fileinfo2 = new FileInfo(installPath + "\\" + "starwarssquadrons_launcher.exe").Length;
                double i = Math.Abs(fileinfo2);
                while (i >= 10)
                    i /= 10;
                if (Directory.Exists(installPath + "\\" + "starwarssquadrons_temp.exe"))
                {
                    if (i >= 5)
                    {
                        dispatcher.DialogShow("Do you want turn on the Easy anti cheat?", DIALOG_TYPE.QUESTION, delegate {
                            File.Move(installPath + "\\" + "starwarssquadrons_launcher.exe", installPath + "\\" + "starwarssquadrons_temp.exe");
                            File.Move(installPath + "\\" + "starwarssquadrons.exe", installPath + "\\" + "starwarssquadrons_launcher.exe");
                            File.Move(installPath + "\\" + "starwarssquadrons_temp.exe", installPath + "\\" + "starwarssquadrons.exe");
                        });
                    }
                    if (i < 5) {
                        dispatcher.DialogShow ("Do you want turn OFF the Easy anti cheat?", DIALOG_TYPE.QUESTION, delegate {
                            File.Move(installPath + "\\" + "starwarssquadrons_launcher.exe", installPath + "\\" + "starwarssquadrons_temp.exe");
                            File.Move(installPath + "\\" + "starwarssquadrons.exe", installPath + "\\" + "starwarssquadrons_launcher.exe");
                            File.Move(installPath + "\\" + "starwarssquadrons_temp.exe", installPath + "\\" + "starwarssquadrons.exe");
                        });
                    }
                }
                else
                {
                    File.Delete(installPath + "\\" + "starwarssquadrons_temp.exe");
                    if (i >= 5)
                    {
                        dispatcher.DialogShow("Do you want turn on the Easy anti cheat?", DIALOG_TYPE.QUESTION, delegate {
                            File.Move(installPath + "\\" + "starwarssquadrons_launcher.exe", installPath + "\\" + "starwarssquadrons_temp.exe");
                            File.Move(installPath + "\\" + "starwarssquadrons.exe", installPath + "\\" + "starwarssquadrons_launcher.exe");
                            File.Move(installPath + "\\" + "starwarssquadrons_temp.exe", installPath + "\\" + "starwarssquadrons.exe");
                        });
                    }
                    if (i < 5)
                    {
                        dispatcher.DialogShow("Do you want turn OFF the Easy anti cheat?", DIALOG_TYPE.QUESTION, delegate {
                            File.Move(installPath + "\\" + "starwarssquadrons_launcher.exe", installPath + "\\" + "starwarssquadrons_temp.exe");
                            File.Move(installPath + "\\" + "starwarssquadrons.exe", installPath + "\\" + "starwarssquadrons_launcher.exe");
                            File.Move(installPath + "\\" + "starwarssquadrons_temp.exe", installPath + "\\" + "starwarssquadrons.exe");

                        });
                        }
                }

            }

           

        



        }


        private void GetBase(string processName,string module = null) {
            Process[] p = Process.GetProcessesByName(processName);
            if(p.Length == 0) {
                dispatcher.DialogShow("Squadrons not running!", DIALOG_TYPE.INFO);
                dispatcher.ExitGame();
                return;
            }
            handle = p[0];
            
            Base = getBase(handle);
            //  Console.WriteLine("Original base: " + Base);
            if (module != null) {
                Base = getBase(handle, module);


            }
        }
        static float readPtr(IntPtr[] offsets, bool debug = false)
        {
            try
            {
                IntPtr tmpptr = (IntPtr)0;
              
                for (int i = 0; i <= offsets.Length - 1; i++)
                {
                    if (i == 0)
                    {
                        if (debug)
                            Console.Write(Base.ToString("X") + "[Base] + " + offsets[i].ToString("X") + "[OFFSET 0]");
                        IntPtr ptr = IntPtr.Add(Base, (int)offsets[i]);
                        tmpptr = (IntPtr)ReadInt64(ptr, 8, handle.Handle);
                        if (debug)
                            Console.WriteLine(" is " + tmpptr.ToString("X"));
                        // Console.WriteLine(GetLastError());
                    }
                    else {
                        if (debug)
                            Console.Write(tmpptr.ToString("X") + " + " + offsets[i].ToString("X") + "[OFFSET " + i + "]");
                        IntPtr ptr2 = IntPtr.Add(tmpptr, (int)offsets[i]);

                        if (i == offsets.Length - 1)
                        {
                            return (BitConverter.ToSingle(ReadBytes((IntPtr)handle.Handle, ptr2, 8), 0));
                        }
                        else
                        {
                            tmpptr = (IntPtr)ReadInt64(ptr2, 8, handle.Handle);
                        }
                        tmpptr = (IntPtr)ReadInt64(ptr2, 8, handle.Handle);
                        if (debug)
                            Console.WriteLine(" is " + tmpptr.ToString("X"));
                    }
                }

            }
            catch (IndexOutOfRangeException)
            {
            }
            catch (InvalidOperationException) { }
            catch (Win32Exception) { }
            return 0;
        }
        static IntPtr getBase(Process handle, string module = null)
        {
            try
            {
                ProcessModuleCollection modules = handle.Modules;
                if (module != null)
                {
                    for (int i = 0; i <= modules.Count - 1; i++)
                    {
                        if (modules[i].ModuleName == module)
                        {
                            return (IntPtr)modules[i].BaseAddress;
                        }
                    }
                    Console.WriteLine("Module Not Found");

                }
                else
                {

                    return (IntPtr)handle.MainModule.BaseAddress;
                }
                return (IntPtr)0;

            }
            catch (Win32Exception e)
            {
                Console.WriteLine(e);
                //dispatcher.ExitGame();
                return (IntPtr)null;
                //MessageBox.Show("Please switch game engine version to 64bit","Error", MessageBoxButtons.OK,MessageBoxIcon.Error,MessageBoxDefaultButton.Button3);
            }
          
        }

        public static byte[] ReadBytes(IntPtr Handle, IntPtr Address, uint BytesToRead)
        {
            IntPtr ptrBytesRead;
            byte[] buffer = new byte[BytesToRead];
            ReadProcessMemory(Handle, Address, buffer, BytesToRead, out ptrBytesRead);

            //Console.WriteLine(GetLastError());
            return buffer;
        }


        public static Int64 ReadInt64(IntPtr Address, uint length = 8, IntPtr? Handle = null)
        {
            return (BitConverter.ToInt64(ReadBytes((IntPtr)Handle, Address, length), 0));
        }

        public static float ClampBetween(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
        public static float NormalizeAngle(float angle)
        {
            float newAngle = angle;
            while (newAngle <= -180) newAngle += 360;
            while (newAngle > 180) newAngle -= 360;
            return newAngle;
        }

        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return null;
        }

        private Stream GetStream(string resourceName)
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
