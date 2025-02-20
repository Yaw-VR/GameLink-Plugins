using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using YawGLAPI;
using ManiaPlanetPlugin.Properties;
using System.Reflection;
using System.IO;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Mania Planet")]
    [ExportMetadata("Version", "1.0")]
    class ManiaPlanetPlugin : Game
    {

        #region structs
        struct Vec3
        {
            public float x, y, z;
        }
        struct Quat
        {
            float w, x, y, z;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct TelemetryStructure
        {
            public struct SHeader
            {
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string Magic;              //  "ManiaPlanet_Telemetry" 0-31
                public UInt32 Version;              // 32-35
                public UInt32 Size;                   // == sizeof(STelemetry) 36-39
            };
            public enum EGameState
            {
                EState_Starting = 0,        //40
                EState_Menus,               //41
                EState_Running,             //42
                EState_Paused,              //43
            };                              //44
            public enum ERaceState
            {
                ERaceState_BeforeState = 0, //45
                ERaceState_Running,         //46
                ERaceState_Finished,        //47
            };
            public struct SGameState
            {
                public EGameState State;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
                public string GameplayVariant;    // player model 'StadiumCar', 'CanyonCar', .... 48-111
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
                public string MapId;             // 112-175
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
                public string MapName;          // 176-431
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string __future__;       // 432-559
            };
            public struct SRaceState
            {
                public ERaceState State;                   //560-563
                public UInt32 Time;                 //564-567
                public UInt32 NbRespawns;               //568-571
                public UInt32 NbCheckpoints;            //572-575
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 498)]
                public string CheckpointTimes; //576-700 ??
                public UInt32 NbCheckpointsPerLap;    // new since Maniaplanet update 2019-10-10; not supported by Trackmania Turbo. 1073-1076
                public UInt32 NbLaps;                 // new since Maniaplanet update 2019-10-10; not supported by Trackmania Turbo. 1077-1080
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
                public string __future__;            //1081-1104
            };
            public struct SObjectState
            {
                public UInt32 Timestamp;                //1105-!1108!
                public UInt32 DiscontinuityCount;     // the number changes everytime the object is moved not continuously (== teleported). 1109-1112
                public Quat Rotation;              //1113-1116-1119-1122-1125
                public Vec3 Translation;            // +x is "left", +y is "up", +z is "front" 1126-1129-1132-1135
                public Vec3 Velocity;               // (world velocity) 1136-1139-1242-1245
                public UInt32 LatestStableGroundContactTime; //1246-1249
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string __future__;            //1250-1281
            };
            public struct SVehicleState
            {
                public UInt32 Timestamp;                //1189-!1192!-1194

                public float InputSteer;               //1195-1199
                public float InputGasPedal;            //1197-1203
                public UInt32 InputIsBraking;            //1201-1204
                public UInt32 InputIsHorn;           //1205-1208

                public float EngineRpm;              // 1500 -> 10000   1211-1215 ?
                public int EngineCurGear;          //842-845	1215-1216!
                public float EngineTurboRatio;       // 1 turbo starting/full .... 0 -> finished 846-849	1217-1224
                public UInt32 EngineFreeWheeling;        // 1225-1228


                public UInt32 WheelsIsGroundContact;      //1229-1232

                public UInt32 WheelsIsSliping;            //1233-1236

                public UInt32 WheelsDamperLen;           //1237-1240
                public float WheelsDamperRangeMin;     //1246-1248
                public float WheelsDamperRangeMax;     //1249-1252

                public float RumbleIntensity;          //1253-1256

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 35)]
                public string offset;            //1250-1281

                public UInt32 SpeedMeter;             // unsigned km/h 1287-1288!!
                public UInt32 IsInWater;                 //1291-1292!-1294
                public UInt32 IsSparkling;
                public UInt32 IsLightTrails;
                public UInt32 IsLightsOn;
                public UInt32 IsFlying;               // long time since touching ground. 1307-1308!-1310

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string __future__;        //1311-1342
            };
            public struct SDeviceState
            {   // VrChair state.
                public Vec3 Euler;                  // yaw, pitch, roll  (order: pitch, roll, yaw) 1343-1347-1351-1354
                public float CenteredYaw;            // yaw accumulated + recentered to apply onto the device	1355-1358
                public float CenteredAltitude;       // Altitude accumulated + recentered	1359-1363

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string __future__;    //1364-1395
            };

            public SHeader Header;

            public UInt32 UpdateNumber;

            public SGameState Game;

            public SRaceState Race;

            public SObjectState Object;

            public SVehicleState Vehicle;

            public SDeviceState Device;
        };
        public static double rad2deg(double radians)
        {
            double degrees = (180 / Math.PI) * radians;
            return (degrees);
        }
        #endregion

        private bool stopThread;
        private Thread readThread;


        public string PROCESS_NAME => "";
        public int STEAM_ID => 0;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => false;

        public string Description => Resources.Description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        public void PatchGame()
        {
            return;
        }
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
 
        private TelemetryStructure MMF2Structure(MemoryMappedFile mmf)
        {
            using (var accessor = mmf.CreateViewAccessor())
            {
                TelemetryStructure read_data;
                int size = Marshal.SizeOf(typeof(TelemetryStructure));
                byte[] data = new byte[size];
                IntPtr p = Marshal.AllocHGlobal(size);
                accessor.ReadArray<byte>(0, data, 0, data.Length);
                Marshal.Copy(data, 0, p, size);
                read_data = (TelemetryStructure)Marshal.PtrToStructure(p, typeof(TelemetryStructure));
                Marshal.FreeHGlobal(p);
                p = IntPtr.Zero;
                return read_data;
            }
        }
        private void ReadFunction()
        {
            Console.WriteLine("Open MMF");
            try
            {
                while (!stopThread)
                {
                    {
                        MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("ManiaPlanet_Telemetry");
                        TelemetryStructure reader =  MMF2Structure(mmf);

                        //Yaw, Pitch
                        float eulerX = Convert.ToSingle(Math.Round(rad2deg(reader.Device.Euler.x), 0)); 
                        float eulerY = Convert.ToSingle(Math.Round(rad2deg(reader.Device.Euler.y), 0)); 
                        float eulerZ = Convert.ToSingle(Math.Round(rad2deg(reader.Device.Euler.z), 0)); 
                        float cyaw = Convert.ToSingle(Math.Round(rad2deg(reader.Device.CenteredYaw), 1)); 
                        float cpitch = Convert.ToSingle(Math.Round(rad2deg(reader.Device.CenteredAltitude), 1)); 

                        controller.SetInput(0, NormalizeAngle(eulerX));
                        controller.SetInput(1, NormalizeAngle(eulerY));
                        controller.SetInput(2, NormalizeAngle(eulerZ));
                        controller.SetInput(3, cyaw);
                        controller.SetInput(4, cpitch);
                    }
                }

            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(Resources.defaultProfile);
        }
        public void Exit()
        {
            stopThread = true;
        }
        public string[] GetInputData()
        {
            return new string[] {
                "eulerX","eulerY","eulerZ","cyaw","cpitch"
            };
        }
        public void Init()
        {

           
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }
        public  LedEffect DefaultLED()
        {
            return new LedEffect(

              EFFECT_TYPE.KNIGHT_RIDER,
              3,
              new YawColor[] {
                new YawColor(66, 135, 245),
                new YawColor(80,80,80),
                new YawColor(128, 3, 117),
                new YawColor(110, 201, 12),
                },
              0.7f);
        }


        private float NormalizeAngle(float angle)
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
