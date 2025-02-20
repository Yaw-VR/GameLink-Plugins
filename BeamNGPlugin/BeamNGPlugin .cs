using BeamNGPlugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{

    //Export attribútum, Exportálja külön állományba
    [Export(typeof(Game))]
    [ExportMetadata("Name", "BeamNG.drive")] //Plugin neve
    [ExportMetadata("Version", "1.0")] //verziószám
    class BeamNGPlugin : Game {

        #region PACKETS
        [StructLayout(LayoutKind.Sequential)]
        public class Packets
        {

            // [FieldOffset(0)]
            public float magic;

            // [FieldOffset(5)]
            public float posX;
            // [FieldOffset(6)]
            public float posY;
            //  [FieldOffset(7)]
            public float posZ;

            //[FieldOffset(8)]
            public float velX;
            // [FieldOffset(9)]
            public float velY;
            // [FieldOffset(10)]
            public float velZ;

            //  [FieldOffset(11)]
            public float accX;
            // [FieldOffset(12)]
            public float accY;
            // [FieldOffset(13)]
            public float accZ;

            // [FieldOffset(14)]
            public float upVecX;
            // [FieldOffset(15)]
            public float upVecY;
            // [FieldOffset(16)]
            public float upVecZ;

            //  [FieldOffset(17)]
            public float rollPos;
            // [FieldOffset(18)]
            public float pitchPos;
            //  [FieldOffset(19)]
            public float yawPos;

            // [FieldOffset(20)]
            public float rollRate;
            // [FieldOffset(21)]
            public float pitchRate;
            //  [FieldOffset(22)]
            public float yawRate;

            //  [FieldOffset(23)]
            public float rollAcc;
            //  [FieldOffset(24)]
            public float pitchAcc;
            // [FieldOffset(25)]
            public float yawAcc;


            /*public bool IsSittingInPits {
                get {
                    return Math.Abs(this.LapTime - 0f) < 1E-05f && Math.Abs(this.Speed - 0f) < 1E-05f;
                }
            }

            public bool IsInPitLane {
                get {
                    return Math.Abs(this.LapTime - 0f) < 1E-05f;
                }
            }

            public string SessionTypeName {
                get {
                    if (Math.Abs(this.SessionType - 9.5f) < 0.0001f) {
                        return "Race";
                    }
                    if (Math.Abs(this.SessionType - 10f) < 0.0001f) {
                        return "Time Trial";
                    }
                    if (Math.Abs(this.SessionType - 170f) < 0.0001f) {
                        return "Qualifying or Practice";
                    }
                    return "Other";
                }
            }
            */
            public float GetPropertyValueAt(int index)
            {
                var prop = this.GetType().GetProperties()[index];
                return (float)prop.GetValue(this, null);
            }
        }

        #endregion

        private IPEndPoint senderIP = new IPEndPoint(IPAddress.Any, 0);
        UdpClient udpClient;
        Thread readThread;
        public int STEAM_ID => 284160;
        public string PROCESS_NAME => "BeamNG.drive";
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => false;
        public string Description => Resources.description;
        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        public int port = 4444;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private bool running = false;

        public void PatchGame()
        {
            return;
        }
        public LedEffect DefaultLED() {

            return dispatcher.JsonToLED(Resources.defProfile);
        }

        public List<Profile_Component> DefaultProfile() {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit() {

            udpClient.Close();
            udpClient = null;
            running = false;
        }

        public string[] GetInputData() {

            Type t = typeof(BeamNGPlugin.Packets);
            FieldInfo[] fields = t.GetFields();

            string[] inputs = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++) {
                inputs[i] = fields[i].Name;
            }
            return inputs;
        }

        public void SetReferences(IProfileManager controller,IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init() {

            udpClient = new UdpClient(4444);
            readThread = new Thread(new ThreadStart(ReadFunction));
            running = true;
            readThread.Start();
        }

        private void ReadFunction() {

            Packets sende = new Packets();
            FieldInfo[] fields = typeof(Packets).GetFields();
            try {


                while (running) {

                    byte[] rawData = udpClient.Receive(ref senderIP);



                    IntPtr unmanagedPointer =
                        Marshal.AllocHGlobal(rawData.Length);
                    Marshal.Copy(rawData, 0, unmanagedPointer, rawData.Length);
                    // Call unmanaged code
                    Marshal.FreeHGlobal(unmanagedPointer);
                    Marshal.PtrToStructure(unmanagedPointer, sende);
                    sende.pitchPos *= 57.295f;
                    sende.rollPos *= 57.295f;
                    sende.yawPos *= 57.295f;

                    for (int i = 0; i < fields.Length; i++) {
                        controller.SetInput(i, (float)fields[i].GetValue(sende));
                    }
                }

            }
            catch (SocketException e) {
                Console.WriteLine(e);

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