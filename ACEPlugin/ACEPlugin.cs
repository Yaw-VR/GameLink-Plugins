using SharedLib;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Assetto Corsa Evo")]
    [ExportMetadata("Version", "1.0")]

    public class ACEPlugin : Game
    {

        #region datastruct
        [StructLayout(LayoutKind.Sequential)]
        public struct Coordinates
        {
            public float X;
            public float Y;
            public float Z;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        [Serializable]
        public struct Physics
        {
            public int PacketId;
            public float Gas;
            public float Brake;
            public float Fuel;
            public int Gear;
            public int Rpms;
            public float SteerAngle;
            public float SpeedKmh;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] Velocity;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] AccG;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] WheelSlip;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] WheelLoad;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] WheelsPressure;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] WheelAngularSpeed;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] TyreWear;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] TyreDirtyLevel;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] TyreCoreTemperature;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] CamberRad;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] SuspensionTravel;

            public float Drs;
            public float TC;
            public float Heading;
            public float Pitch;
            public float Roll;
            public float CgHeight;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public float[] CarDamage;

            public int NumberOfTyresOut;
            public int PitLimiterOn;
            public float Abs;

            public float KersCharge;
            public float KersInput;
            public int AutoShifterOn;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public float[] RideHeight;

            // since 1.5
            public float TurboBoost;
            public float Ballast;
            public float AirDensity;

            // since 1.6
            public float AirTemp;
            public float RoadTemp;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] LocalAngularVelocity;
            public float FinalFF;

            // since 1.7
            public float PerformanceMeter;
            public int EngineBrake;
            public int ErsRecoveryLevel;
            public int ErsPowerLevel;
            public int ErsHeatCharging;
            public int ErsisCharging;
            public float KersCurrentKJ;
            public int DrsAvailable;
            public int DrsEnabled;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] BrakeTemp;

            // since 1.10
            public float Clutch;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] TyreTempI;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] TyreTempM;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] TyreTempO;

            // since 1.10.2
            public int IsAIControlled;

            // since 1.11
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Coordinates[] TyreContactPoint;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Coordinates[] TyreContactNormal;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Coordinates[] TyreContactHeading;
            public float BrakeBias;

            // since 1.12
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] LocalVelocity;
        }
        #endregion

        private MemoryMappedFile mmf;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private Thread readThread;
        private bool running = false;
        public int STEAM_ID => 3058630;
        public string PROCESS_NAME => "AssettoCorsaEVO";
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "YawVR";


        public Stream Logo => ResourceHelper.GetStream("logo.png");
        public Stream SmallLogo => ResourceHelper.GetStream("recent.png");
        public Stream Background => ResourceHelper.GetStream("wide.png");
        public string Description => ResourceHelper.GetString("description.html");

        private string defProfilejson => ResourceHelper.GetString("Default.yawglprofile");

        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(defProfilejson);


        private static readonly string[] inputs = {
            "RPM","Speed","Gas","Brake","TurboBoost","accG_X","accG_Y","accG_Z","Heading","Pitch","Roll"
        };

        public void Exit()
        {
            running = false;
        }
        public string[] GetInputData()
        {

            return inputs;
        }
        public LedEffect DefaultLED()
        {

            return new LedEffect(

                EFFECT_TYPE.KNIGHT_RIDER,
                0,
                new YawColor[] {
                new YawColor(255,40,0),
                new YawColor(80,80,80),
                new YawColor(255, 100, 0),
                new YawColor(140, 0, 255),
                },
                0.004f);
        }
        
     

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;

        }
        public void Init()
        {
            running = true;
            readThread = new Thread(new ThreadStart(ReadThread));
            readThread.Start();

        }

        private void ReadThread()
        {
            while (!OpenMMF())
            {
                Console.WriteLine("Cant connect to MMF, trying again..");
                Thread.Sleep(2000);

            }

            while (running)
            {
                Physics p = ReadPhysics();
                controller.SetInput(0, p.Rpms);
                controller.SetInput(1, p.SpeedKmh);
                controller.SetInput(2, p.Gas);
                controller.SetInput(3, p.Brake);
                controller.SetInput(4, p.TurboBoost);

                controller.SetInput(5, p.AccG[0]);
                controller.SetInput(6, p.AccG[1]);
                controller.SetInput(7, p.AccG[2]);

                controller.SetInput(8, p.Heading * 57.2957795f);
                controller.SetInput(9, p.Pitch * 57.2957795f);
                controller.SetInput(10, p.Roll * 57.2957795f);
                Thread.Sleep(20);
            }
        }



        private bool OpenMMF()
        {
            try
            {
                mmf = MemoryMappedFile.OpenExisting("Local\\acpmf_physics");
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }



        /// <summary>
        /// Read the current physics data from shared memory
        /// </summary>
        /// <returns>A Physics object representing the current status, or null if not available</returns>
        public Physics ReadPhysics()
        {
            using (var stream = mmf.CreateViewStream())
            {
                using (var reader = new BinaryReader(stream))
                {
                    var size = Marshal.SizeOf(typeof(Physics));
                    var bytes = reader.ReadBytes(size);
                    var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    var data = (Physics)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Physics));
                    handle.Free();
                    return data;
                }
            }
        }

        public void PatchGame()
        {
            return;
        }

        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return null;
        }

        public Type GetConfigBody()
        {
            return null;
        }
    }
       
}
