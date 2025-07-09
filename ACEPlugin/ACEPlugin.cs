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

namespace ACEPlugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Assetto Corsa Evo")]
    [ExportMetadata("Version", "1.2")]

    public class ACEPlugin : Game
    {

      
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
            "RPM","Speed","Gas","Brake","TurboBoost","accG_X","accG_Y","accG_Z","Heading","Pitch","Roll",

            "LocalVelocityX","LocalVelocityY","LocalVelocityZ",
            "WheelSlip0","WheelSlip1","WheelSlip2","WheelSlip3",
            "WheelAngularSpeed0", "WheelAngularSpeed1", "WheelAngularSpeed2", "WheelAngularSpeed3",
            "ABS","Boost","Ballast",
            "LocalAngularV_X", "LocalAngularV_Y", "LocalAngularV_Z",
            "SuspensionTravel0","SuspensionTravel1","SuspensionTravel2","SuspensionTravel3",
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

                controller.SetInput(11, p.LocalVelocity[0]);
                controller.SetInput(12, p.LocalVelocity[0]);
                controller.SetInput(13, p.LocalVelocity[0]);

                controller.SetInput(14,p.WheelSlip[0]);
                controller.SetInput(15,p.WheelSlip[1]);
                controller.SetInput(16,p.WheelSlip[2]);
                controller.SetInput(17,p.WheelSlip[3]);

                controller.SetInput(18, p.WheelAngularSpeed[0]);
                controller.SetInput(19, p.WheelAngularSpeed[1]);
                controller.SetInput(20, p.WheelAngularSpeed[2]);
                controller.SetInput(21, p.WheelAngularSpeed[3]);

                controller.SetInput(22, p.Abs);
                controller.SetInput(23, p.TurboBoost);
                controller.SetInput(24, p.Ballast);

                controller.SetInput(25, p.LocalAngularVelocity[0]);
                controller.SetInput(26, p.LocalAngularVelocity[1]);
                controller.SetInput(27, p.LocalAngularVelocity[2]);

                controller.SetInput(28, p.SuspensionTravel[0]);
                controller.SetInput(29, p.SuspensionTravel[1]);
                controller.SetInput(30, p.SuspensionTravel[2]);
                controller.SetInput(31, p.SuspensionTravel[2]);

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
