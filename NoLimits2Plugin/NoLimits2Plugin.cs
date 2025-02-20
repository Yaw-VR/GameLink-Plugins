using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Nolimits 2")]
    [ExportMetadata("Version", "1.3")]


    class NoLimits2Plugin : Game {

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }





        public string PROCESS_NAME => "nolimits2stm";
        public int STEAM_ID => 301320;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => true;


        public string Description => GetString("description.html");

        private string defProfile => GetString("Default.yawglprofile");
        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        private int N_MSG_GET_TELEMETRY = 5;
        //  private int N_MSG_TELEMETRY = 6;
        private int N_MSG_SET_PAUSE = 27;
        private int N_MSG_RESET_PARK = 28;
        private int s_nRequestId;


        public bool Paused { get; set; }
        private int c_nExtraSizeOffset = 9;
        bool stopThread = false;

        private TcpClient tcpClient;
       // private UdpClient udpClient;

        Thread readThread;
        public void Exit() {
            tcpClient.Close();
            tcpClient = null;
            stopThread = true;
            //readThread.Abort();
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
        public void Init() {
            
            stopThread = false;
            tcpClient = new TcpClient();
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }


        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            var dict = new Dictionary<string, ParameterInfo[]>();

            ParameterInfo[] cPause = typeof(NoLimits2Plugin).GetMethod(nameof(Pause)).GetParameters();
            ParameterInfo[] cReset = typeof(NoLimits2Plugin).GetMethod(nameof(Reset)).GetParameters();

            dict.Add(nameof(Pause), cPause);
            dict.Add(nameof(Reset), cReset);
            return dict;
        }

        public async void Pause(float delay)
        {

            byte[] telemetry_request = CreateBoolMessage(s_nRequestId++, N_MSG_SET_PAUSE,!Paused);

            if(Paused) await Task.Delay(TimeSpan.FromMilliseconds(delay));
            tcpClient.Client.Send(telemetry_request);
        }

        public void Reset()
        {
            byte[] telemetry_request = CreateBoolMessage(s_nRequestId++, N_MSG_RESET_PARK, true);
            tcpClient.Client.Send(telemetry_request);
        }
        private void ReadFunction()  {
            Debug.WriteLine("NoLimits2 read thread started");

            while (!stopThread)
            {
                try
                {
                    if (!tcpClient.Connected)
                    {
                        tcpClient.Connect("127.0.0.1", 15151);
                    }

                        byte[] telemetry_request = createSimpleMessage(s_nRequestId++, N_MSG_GET_TELEMETRY);


                        tcpClient.Client.Send(telemetry_request);

                        byte[] buffer = new byte[512];

                        tcpClient.Client.Receive(buffer, 0, 512, SocketFlags.None);
                        DecodeTelemetry(buffer);
                    

                } catch(Exception e)
                {

                }
                Thread.Sleep(15);
            }


        }






        private byte[] CreateBoolMessage(int requestId, int msgEnum, bool bVal)
        {
            byte[] msg = CreateComplexMessage(requestId, msgEnum, 1);
            encodeBoolean(msg, c_nExtraSizeOffset, bVal);
            return msg;
        }


        /**
   * Encode b as one byte
   */
        private void encodeBoolean(byte[] msg, int offset, bool b)
        {
            msg[offset] = (byte)(b ? 1 : 0);
        }
        /**
       * Create a message with DataSize=0
       */
        private byte[] createSimpleMessage(int requestId, int msgEnum) {
            byte[] msg = new byte[10];
            msg[0] = (byte)'N';
            encodeUShort16(msg, 1, msgEnum);
            encodeInt32(msg, 3, requestId);
            encodeUShort16(msg, 7, 0);
            msg[9] = (byte)'L';
            return msg;
        }



        private static byte[] CreateComplexMessage(int requestId, int msgEnum, int extraSize)
        {
            if (extraSize < 0 || extraSize > 65535) return null;
            byte[] msg = new byte[10 + extraSize];
            msg[0] = (byte)'N';
            encodeUShort16(msg, 1, msgEnum);
            encodeInt32(msg, 3, requestId);
            encodeUShort16(msg, 7, extraSize);
            msg[9 + extraSize] = (byte)'L';
            return msg;
        }

        /**
          * Encode n as two bytes (ushort16/sshort16) in network byte order (big-endian)
          */
        private static void encodeUShort16(byte[] msg, int offset, int n) {
            msg[offset] = (byte)((n >> 8) & 0xFF);
            msg[offset + 1] = (byte)(n & 0xFF);
        }

        /**
         * Encode n as four bytes (uint32/int32) in network byte order (big-endian)
         */
        private static void encodeInt32(byte[] msg, int offset, int n) {
            msg[offset] = (byte)((n >> 24) & 0xFF);
            msg[offset + 1] = (byte)((n >> 16) & 0xFF);
            msg[offset + 2] = (byte)((n >> 8) & 0xFF);
            msg[offset + 3] = (byte)(n & 0xFF);
        }
        /**
       * Decode four bytes in network byte order (big-endian) as int32
       */
        private static int decodeInt32(byte[] msg, int offset) {
            int n1 = (((int)msg[offset]) & 0xFF) << 24;
            int n2 = (((int)msg[offset + 1]) & 0xFF) << 16;
            int n3 = (((int)msg[offset + 2]) & 0xFF) << 8;
            int n4 = ((int)msg[offset + 3]) & 0xFF;
            return n1 | n2 | n3 | n4;
        }

        /**
         * Decode four bytes in network byte order (big-endian) as float32
         */
        private static float decodeFloat(byte[] msg, int offset) {
            byte[] array = new byte[4];

            for (int i = 0; i < 3; i++) {
                array[i] = msg[offset + i];
            }

            return floatConversion(array);

        }

        private static float floatConversion(byte[] bytes) {
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(bytes); // Convert big endian to little endian
            }
            float myFloat = BitConverter.ToSingle(bytes, 0);
            return (float)Math.Round(myFloat, 3);
        }


        private int prevState = 0;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        private void DecodeTelemetry(byte[] bytes) {
            //  int size = bytes.Length;
            //   if (size == 76) {
            int state = decodeInt32(bytes, c_nExtraSizeOffset);
            if(prevState == 0 && state == 3) { //entered simulation mode
                controller.ResetYawOffset();
            }
            prevState = state;
            int frameNo = decodeInt32(bytes, c_nExtraSizeOffset + 4);

            bool inPlay = (state & 1) != 0;
            bool onboard = (state & 2) != 0;
            Paused = (state & 4) != 0;

            int viewMode = decodeInt32(bytes, c_nExtraSizeOffset + 8);
            int coasterIndex = decodeInt32(bytes, c_nExtraSizeOffset + 12);
            int coasterStyleId = decodeInt32(bytes, c_nExtraSizeOffset + 16);
            int currentTrain = decodeInt32(bytes, c_nExtraSizeOffset + 20);
            int currentCar = decodeInt32(bytes, c_nExtraSizeOffset + 24);
            int currentSeat = decodeInt32(bytes, c_nExtraSizeOffset + 28);
            
            
            float speed = decodeFloat(bytes, c_nExtraSizeOffset + 32);

            float posx = decodeFloat(bytes, c_nExtraSizeOffset + 36);
            float posy = decodeFloat(bytes, c_nExtraSizeOffset + 40);
            float posz = decodeFloat(bytes, c_nExtraSizeOffset + 44);

            Quaternion quater = new Quaternion();
            quater.x = decodeFloat(bytes, c_nExtraSizeOffset + 48);
            quater.y = decodeFloat(bytes, c_nExtraSizeOffset + 52);
            quater.z = decodeFloat(bytes, c_nExtraSizeOffset + 56);
            quater.w = decodeFloat(bytes, c_nExtraSizeOffset + 60);

            //float yaw = (float)RadianToDegree(quater.toYawFromYUp()) * -1;
            //float pitch = (float) RadianToDegree(quater.toPitchFromYUp()) * -1;
            //float roll = (float) RadianToDegree(quater.toRollFromYUp());
            var (pitch, yaw, roll) = ToPitchYawRoll(quater);


            speed = (byte)Math.Round(Math.Pow(speed, 2) / 10, 0);

         //   controller.SetBuzzer(buzzerAmp, buzzerAmp, buzzerAmp, 17);
            //  orientation = QuaternionToEuler(quater);
            float gforcex = decodeFloat(bytes, c_nExtraSizeOffset + 64);
            float gforcey = decodeFloat(bytes, c_nExtraSizeOffset + 68);
            float gforcez = decodeFloat(bytes, c_nExtraSizeOffset + 72);

            

            controller.SetInput(0, speed);
            controller.SetInput(1, -yaw);
            controller.SetInput(2, -pitch);
            controller.SetInput(3, -roll);

            controller.SetInput(4, gforcex);
            controller.SetInput(5, gforcey);
            controller.SetInput(6, gforcez);


        }




        private static double RadianToDegree(double angle) {
            return angle * (180.0 / Math.PI);
        }

        public string[] GetInputData() {
            return new string[] {
                "Speed",
                "Yaw",
                "Pitch",
                "Roll",
                "Force_X",
                "Force_Y",
                "Force_Z" };
        }

        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(defProfile);

        public LedEffect DefaultLED() {
            return new LedEffect(
                
                EFFECT_TYPE.FLOW_LEFTRIGHT,
                3,
                new YawColor[] {
                    new YawColor(52, 235, 192),
                    new YawColor(20,20,20),
                    new YawColor(235, 52, 137),
                    new YawColor(235, 165, 52),
                     },
                 1f);

                
        }

        public void PatchGame() {
            try
            {
                string exePath = dispatcher.GetInstallPath("Nolimits 2 Roller Coaster Simulation") + "/64bit/nolimits2stm.exe";
                CreateShortcut(exePath, "NoLimits2wTelemetry", "NoLimits2 start with telemetry server", "--telemetry");
                dispatcher.DialogShow("A shortcut has been placed on your desktop. Please launch the game with this shortcut to enable telemetry output.", DIALOG_TYPE.INFO);
            }
            catch (Exception ex)
            {
                dispatcher.DialogShow($"Error patching Nolimits2: {ex}", DIALOG_TYPE.INFO);
            }
    
        }

        public static void CreateShortcut(string exePath, string linkname, string description, string args)
        {
            IShellLink link = (IShellLink)new ShellLink();

            // setup shortcut information
            link.SetDescription(description);
            link.SetPath(exePath);
            link.SetArguments(args);
            // save it
            IPersistFile file = (IPersistFile)link;
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            file.Save(Path.Combine(desktopPath, linkname + ".lnk"), false);
        }

        const float Rad2Degf = 57.29578f;
        (float pitch, float yaw, float roll) ToPitchYawRoll(Quaternion q)
        {
            var yaw = (float)Math.Atan2(2 * (q.y * q.w - q.x * q.z), 1 - 2 * (q.y * q.y + q.z * q.z)) * Rad2Degf;
            var pitch = (float)Math.Atan2(2 * (q.x * q.w - q.y * q.z), 1 - 2 * (q.x * q.x + q.z * q.z)) * Rad2Degf;
            var roll = (float)Math.Asin(2 * (q.x * q.y + q.z * q.w)) * Rad2Degf;

            return (pitch, yaw, -roll);
        }

        Stream GetStream(string resourceName)
        {
            var assembly = GetType().Assembly;
            var rr = assembly.GetManifestResourceNames();
            string fullResourceName = $"{assembly.GetName().Name}.Resources.{resourceName}";
            return assembly.GetManifestResourceStream(fullResourceName);
        }

        string GetString(string resourceName)
        {

            var result = string.Empty;

            using var stream = GetStream(resourceName);

            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                result = reader.ReadToEnd();
            }


            return result;
        }

        public Type GetConfigBody()
        {
            return null;
        }
    }
}
