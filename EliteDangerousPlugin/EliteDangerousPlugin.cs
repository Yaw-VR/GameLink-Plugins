using EliteDangerousPlugin;
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
using System.Text.RegularExpressions;
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

        Config pConfig;
        private int m_nYawId = -1;
        private int m_nPitchId = -1;
        private int m_nRollId = -1;
        private float m_fYawVelocity = 0.0f;
        private float m_fPitchVelocity = 0.0f;
        private float m_fRollVelocity = 0.0f;

        private int m_nYawLimitedId = -1;
        private int m_nPitchLimitedId = -1;
        private int m_nRollLimitedId = -1;
        private float m_fYawPosition = 0.0f;
        private float m_fPitchPosition = 0.0f;
        private float m_fRollPosition = 0.0f;
        private float MoveToZeroSpeed = 0.05f;

        float RollLimitMin = -15.0f;
        float RollLimitMax = 15.0f;

        float PitchLimitMax = 30.0f;
        float PitchLimitMin = -30.0f;

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
            
            // Yaw/Pitch/Roll Limited hozzá adása
            m_nYawId = Array.FindIndex(this.inputs, item => item.Equals("Yaw"));
            m_nPitchId = Array.FindIndex(this.inputs, item => item.Equals("Pitch"));
            m_nRollId = Array.FindIndex(this.inputs, item => item.Equals("Roll"));

            m_nYawLimitedId = inputs.Count + 0;
            m_nPitchLimitedId = inputs.Count + 1;
            m_nRollLimitedId = inputs.Count + 2;
            inputs.Add("Yaw_Limited");
            inputs.Add("Pitch_Limited");
            inputs.Add("Roll_Limited");

            // set
            this.inputs = inputs.ToArray();
        }
        public void Init() {

            Console.WriteLine("STARTED ELITE PLUGIN");

            pConfig = dispatcher.GetConfigObject<Config>();

            m_fYawPosition = 0.0f;
            m_fPitchPosition = 0.0f;
            m_fRollPosition = 0.0f;
            m_fYawVelocity = 0.0f;
            m_fPitchVelocity = 0.0f;
            m_fRollVelocity = 0.0f;
            MoveToZeroSpeed = pConfig.MoveToZeroSpeed;

            IDeviceParameters deviceParameters = dispatcher.GetDeviceParameters();
            RollLimitMin = -deviceParameters.RollLimit;
            RollLimitMax = deviceParameters.RollLimit;
            PitchLimitMax = deviceParameters.PitchLimitB;
            PitchLimitMin = -deviceParameters.PitchLimitF;

            running = true;
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();

        }

        float ToRadian(float fDegree)
        {
            float fRandian = (fDegree / 180.0f) * 3.141592654f;
            return fRandian;
        }

        float ExponentialCurve(float x)
        {
            float x2 = 90.0f - x;

            float k = pConfig.K;
            float y = (float)(1.0 - Math.Exp(-k * (1.0 - x2 / 90.0)));
            return y;
        }

        float Lerp(float a, float b, float t)
        {
            return (a + (b - a) * t);
        }

        float Calc(float fPosition, float fVelocity, float LimitMin, float LimitMax) 
        {
            if (fPosition > LimitMax) { fPosition = LimitMax; }
            if (fPosition < LimitMin) { fPosition = LimitMin; }

            float pos2 = fPosition;
            if (fPosition > 0.0f)
            {
                if (fVelocity > 0.01f) // emelkedik
                {
                    float degree = fPosition / LimitMax; // [0.0 .. 1.0]
                    degree *= 90.0f; // [0.0 .. 90]
                    float weight = ExponentialCurve(degree); // [0.0 .. 1.0]
                    pos2 = weight * LimitMax;

                    if (Math.Abs(pos2) > Math.Abs(fPosition)) { pos2 = fPosition; } // nem lehet nagyobb mint a lineáris
                    fPosition = pos2;
                }
                else // lerp
                {
                    fPosition = Lerp(fPosition, 0.0f, MoveToZeroSpeed);
                }
            }
            else if (fPosition < 0.0f)
            {
                if (fVelocity < -0.01f) // süllyed
                {
                    float degree = Math.Abs(fPosition) / Math.Abs(LimitMin); // [0.0 .. 1.0]
                    degree *= 90.0f; // [0.0 .. 90]
                    float weight = ExponentialCurve(degree); // [0.0 .. 1.0]
                    pos2 = weight * LimitMin;

                    if (Math.Abs(pos2) > Math.Abs(fPosition)) { pos2 = fPosition; } // nem lehet nagyobb mint a lineáris
                    fPosition = pos2;
                }
                else // lerp
                {
                    fPosition = Lerp(fPosition, 0.0f, MoveToZeroSpeed);
                }
            }

            return fPosition;
        }


        private void ReadFunction() {
            try {
                GetBase(PROCESS_NAME);
                while (running) 
                {
                    if (handle != null)
                    {
                        if (Base != null)
                        {
                            for (int i = 0; i < inputAddrs.Length; i++)
                            {
                                float fValue = readPtr(inputAddrs[i], false);
                                controller.SetInput(i, fValue);

                                if (i == m_nYawId) { m_fYawVelocity = fValue; }
                                if (i == m_nPitchId) { m_fPitchVelocity = -fValue; }
                                if (i == m_nRollId) { m_fRollVelocity = fValue; }
                            }
                        }
                    } 
                    else
                    {
                        Thread.Sleep(1000);
                        GetBase(PROCESS_NAME);
                    }

                    // Yaw/Pitch/Roll Limited kiszámítása
                    // -> Yaw
                    if (Math.Abs(m_fYawVelocity) > pConfig.MaxYawVelocity)
                    {
                        m_fYawVelocity = Math.Sign(m_fYawVelocity) * pConfig.MaxYawVelocity;
                    }
                    m_fYawPosition += m_fYawVelocity;

                    // -> Pitch
                    if (Math.Abs(m_fPitchVelocity) > pConfig.MaxPitchVelocity)
                    {
                        m_fPitchVelocity = Math.Sign(m_fPitchVelocity) * pConfig.MaxPitchVelocity;
                    }
                    m_fPitchPosition += m_fPitchVelocity;
                    m_fPitchPosition = Calc(m_fPitchPosition, m_fPitchVelocity, PitchLimitMin, PitchLimitMax);

                    // -> Roll
                    if (Math.Abs(m_fRollVelocity) > pConfig.MaxRollVelocity)
                    {
                        m_fRollVelocity = Math.Sign(m_fRollVelocity) * pConfig.MaxRollVelocity;
                    }
                    m_fRollPosition += m_fRollVelocity;
                    m_fRollPosition = Calc(m_fRollPosition, m_fRollVelocity, RollLimitMin, RollLimitMax);

                    // set
                    controller.SetInput(m_nYawLimitedId, m_fYawPosition);
                    controller.SetInput(m_nPitchLimitedId, -m_fPitchPosition);
                    controller.SetInput(m_nRollLimitedId, m_fRollPosition);

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
            return typeof(Config);
        }
    }



}
