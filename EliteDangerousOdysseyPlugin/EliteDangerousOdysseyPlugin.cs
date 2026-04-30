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
using SharedLib;
using System.Xml.Linq;
using Newtonsoft.Json;
namespace YawVR_Game_Engine.Plugin
{



    [Export(typeof(Game))]
    [ExportMetadata("Name", "Elite Dangerous Odyssey")]
    [ExportMetadata("Version", "1.8")]
    class EliteDangerousOdysseyPlugin : Game {
   
     
        public int STEAM_ID => 359320;
        public string PROCESS_NAME => "EliteDangerous64";
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "Drowhunter";

        
        public Stream Logo => ResourceHelper.Logo;
        public Stream SmallLogo => ResourceHelper.SmallLogo;
        public Stream Background => ResourceHelper.Background;

        public string Description => ResourceHelper.GetString("description.html");

        private string defProfilejson => ResourceHelper.GetString("Default.yawglprofile");

        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;


        EliteConfig settings = new EliteConfig();
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

        public LedEffect DefaultLED() => dispatcher.JsonToLED(defProfilejson);

        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(defProfilejson);

        public void Exit() {
            running = false;
        }

        public string[] GetInputData() {
#if DEBUG
            Debugger.Launch();

#endif
            
            
            LoadOffsets("Odyssey");

            return inputs;
        }

        /// <summary>
        /// Convert EliteDangerous64_Offsets.xml to JSon in the format of the object file, this is done to allow users to easily add their own offsets file without needing to convert it to json themselves. The xml file is in the format of the one provided by the elite dangerous memory layout github repo, and the json file is in the format of the object file used by yawglapi.
        /// </summary>
        /// <param name="xmlPath"></param>
        /// <param name="isHorizons"></param>
        /// <returns></returns>
        private JObject OffsetXMLToJobject(string xmlPath, bool isHorizons, int version = 0)
        {
            if (File.Exists(xmlPath))
            {
                XDocument doc = XDocument.Load(xmlPath);
                JObject jObject = new JObject();

                XElement root = doc.Root;
                if (root == null)
                {
                    dispatcher.ShowNotification(NotificationType.ERROR, "Offsets XML is invalid: missing root element.");
                    return null;
                }

                string modeName = isHorizons ? "Horizons" : "Odyssey";
                XElement modeNode = root.Element(modeName);
                if (modeNode == null)
                {
                    dispatcher.ShowNotification(NotificationType.ERROR, $"Offsets XML is missing '{modeName}' section.");
                    return null;
                }

                foreach (XElement pointerNode in modeNode.Elements())
                {
                    XAttribute offsetsCountAttribute = pointerNode.Attribute("Offsets");
                    if (offsetsCountAttribute == null || !int.TryParse(offsetsCountAttribute.Value, out int offsetsCount))
                    {
                        continue;
                    }

                    JArray offsets = new JArray();
                    for (int i = 0; i < offsetsCount; i++)
                    {
                        XAttribute offsetAttribute = pointerNode.Attribute($"Offset_{i}");
                        if (offsetAttribute == null)
                        {
                            break;
                        }

                        offsets.Add(offsetAttribute.Value.Trim());
                    }

                    if (offsets.Count > 0)
                    {
                        jObject[pointerNode.Name.LocalName] = new JObject
                        {
                            ["Offsets"] = offsets
                        };
                    }
                }

                return new JObject
                {
                    ["version"] = version+"",
                    ["data"] = jObject
                };
            }

            dispatcher.ShowNotification(NotificationType.ERROR, "Offsets file not found, plugin will not work. Please download 'EliteDangerous64_Offsets.xml' file to plugin folder.");
            return null;
        }

        public void SetReferences(IProfileManager controller,IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;



        }

        private void LoadOffsets(string game)
        {
            //JObject objectFileData;

            //dispatcher.GetObjectFile("elitedangerous", out objectFileData);

            string offsetsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "EliteDangerous64_Offsets.xml");
            //string existingVersion = objectFileData?["version"]?.ToString() ?? "9";
            JObject objectFileData = OffsetXMLToJobject(offsetsPath, game == "Horizons", 8);

            if (objectFileData == null)
                return;
            

            var of = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "EliteDangerous64_Offsets.json");

            File.WriteAllText(of, JsonConvert.SerializeObject(objectFileData, Formatting.Indented));


            JObject dataNode = (objectFileData?["data"] as JObject) ?? objectFileData;
            if (dataNode != null)
            {
                SetupInputs(dataNode);
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

#if DEBUG
            Debugger.Launch();

#endif
            
            //this.settings = dispatcher.GetConfigObject<EliteConfig>();
            Console.WriteLine("STARTED ELITE ODYSSEY PLUGIN");
            running = true;
          
    
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();

        }

        private void ReadFunction() {
            try {
                GetBase(PROCESS_NAME);
                settings = dispatcher.GetConfigObject<EliteConfig>();
                LoadOffsets("Odyssey");

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

        

        public Type GetConfigBody()
        {
            return typeof(EliteConfig);
        }
    }

    public struct EliteConfig
    {
        [Info(Description = "Game Version Horizons or Odyssey?", Name = "Game Version", RegexValidator = "Odyssey")]

        public string Game;

        public EliteConfig()
        {
            Game = "Odyssey";
        }
    }

}
