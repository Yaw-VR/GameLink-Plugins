using EliteDangerousPlugin.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{



    [Export(typeof(Game))]
    [ExportMetadata("Name", "Elite Dangerous")]
    [ExportMetadata("Version", "1.7")]
    class EliteDangerousPlugin : Game {
   
     
        public int STEAM_ID => 359320;
        public string PROCESS_NAME => "EliteDangerous64";
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "YawVR";

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");

        public Stream Background => GetStream("wide.png");

        public string Description => string.Empty;

        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;



        private bool running = false;
        private Thread readThread;
        private Process handle;
        private IntPtr Base;

        private string[] inputs = new string[0];

        private IntPtr[][] inputAddrs;

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        [DllImport("kernel32.dll")]
        public static extern IntPtr ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            [In, Out] byte[] buffer, UInt32 size, out IntPtr lpNumberOfBytesRead);
  
        public List<Profile_Component> DefaultProfile() {


            return dispatcher.JsonToComponents(Resources.defProfile);
        }
        public LedEffect DefaultLED() {

            return dispatcher.JsonToLED(Resources.defProfile);
        }

        public void Exit() {
            running = false;
        }

        public string[] GetInputData() {
            return inputs;
        }


        public void SetReferences(IProfileManager controller,IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
            JObject objectFileData;
            dispatcher.GetObjectFile("elitedangerous", out objectFileData);
            if (objectFileData != null)
            {
                SetupInputs(objectFileData);
            }

        }

        private void SetupInputs(JObject objectFileData)
        {

            List<string> inputs = new List<string>();

            inputAddrs = new IntPtr[objectFileData.Properties().Count()][];

            int counter = 0;


            foreach(var obj in objectFileData)
            {
                inputs.Add($"{obj.Key}");

                var offsets = obj.Value["Offsets"].ToArray();
                inputAddrs[counter] = new IntPtr[offsets.Length];
                for(int i =0;i< offsets.Length; i++)
                {
                    string v = offsets[i].ToString();
                    inputAddrs[counter][i] = (IntPtr)int.Parse(v, System.Globalization.NumberStyles.HexNumber);
                }
                counter++;
            }
            this.inputs = inputs.ToArray();


        }
        public void Init() {

            Console.WriteLine("STARTED ELITE PLUGIN");
            running = true;
          
    
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();

        }

        private void ReadFunction() {
            try {
                GetBase(PROCESS_NAME);
                while (running) {
                    if (handle != null)
                    {
                        if (Base != null)
                        {
                            for (int i = 0; i < inputAddrs.Length; i++)
                            {
                                controller.SetInput(i, readPtr(inputAddrs[i], false));
                            }
                        }
                    } else
                    {
                        Thread.Sleep(1000);
                        GetBase(PROCESS_NAME);
                    }

                    // Console.WriteLine(string.Format("Yaw: {0:0.00} \n Pitch: {1:0.00} \n Roll {2:0.00}", Yaw, Pitch, Roll)
                    Thread.Sleep(20);

                }

            }
            catch (Exception) {
                dispatcher.ExitGame();
            }
        }

        public void PatchGame()
        {
            return;
        }
        private bool GetBase(string processName) {
            Process[] p = Process.GetProcessesByName(processName);
            if (p.Length == 0) {
                return false;
            }
            handle = p[0];

            Base = getBase(handle);

            return true;      
        }

        float readPtr(IntPtr[] offsets, bool debug = false, string module = null) {
            try {
                IntPtr tmpptr = (IntPtr)0;
              
                for (int i = 0; i <= offsets.Length - 1; i++) {
                    if (i == 0) {
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

                        if (i == offsets.Length - 1) {
                            return (BitConverter.ToSingle(ReadBytes((IntPtr)handle.Handle, ptr2, 8), 0));
                        }
                        else {
                            tmpptr = (IntPtr)ReadInt64(ptr2, 8, handle.Handle);
                        }
                        tmpptr = (IntPtr)ReadInt64(ptr2, 8, handle.Handle);
                        if (debug)
                            Console.WriteLine(" is " + tmpptr.ToString("X"));
                        //Console.WriteLine(GetLastError());
                    }
                }

            } catch (IndexOutOfRangeException) {
            } catch (InvalidOperationException) { }
            catch (Win32Exception) { }
            return 0;
            }
        IntPtr getBase(Process handle, string module = null)
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
                Console.WriteLine("zero error");
                return (IntPtr)0;

            }
            catch (Win32Exception e)
            {
                Console.WriteLine(e);
               // Form1.Instance.ErrorHappened(new Exception("Please switch game engine version to 64bit"));
                return (IntPtr)null;
            }
        }
           

        public static byte[] ReadBytes(IntPtr Handle, IntPtr Address, uint BytesToRead) {
            IntPtr ptrBytesRead;
            byte[] buffer = new byte[BytesToRead];
            ReadProcessMemory(Handle, Address, buffer, BytesToRead, out ptrBytesRead);

            //Console.WriteLine(GetLastError());
            return buffer;
        }


        public static Int64 ReadInt64(IntPtr Address, uint length = 8, IntPtr? Handle = null) {
            return (BitConverter.ToInt64(ReadBytes((IntPtr)Handle, Address, length), 0));
        }


        public static float Clamp(float v, float limit)
        {
            if (limit == -1) return v;
            if (v > limit) return limit;
            if (v < -limit) return -limit;
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
