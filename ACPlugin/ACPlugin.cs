using ACPlugin.Properties;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;
using YawGLAPI;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Assetto Corsa (PC)")]
    [ExportMetadata("Version", "1.3")]
    class ACPlugin : Game
    {
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;

        private Thread readThread;
        private bool running = false;
        private MemoryMappedFile sharedMemory;
        private MemoryMappedViewAccessor viewAccessor;

        public string PROCESS_NAME => "acs";
        public int STEAM_ID => 244210;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => false;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");
        public string Description => String.Empty;

        public void PatchGame()
        {
            return;
        }

        public void Exit()
        {
            running = false;
            readThread?.Join();
            viewAccessor?.Dispose();
            sharedMemory?.Dispose();
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }

        public void Init()
        {
            bool mmfFound = false;
            running = true;
            Task.Run(async delegate
            {
                do
                {
                    try
                    {
                        sharedMemory = MemoryMappedFile.OpenExisting("Local\\acpmf_physics");
                        viewAccessor = sharedMemory.CreateViewAccessor();

                        mmfFound = true;
                        readThread = new Thread(ReadFunction);
                        readThread.Start();
                    }
                    catch (FileNotFoundException)
                    {
                        //Keep trying, unless plugin is stopped
                        await Task.Delay(1000);
                    }
                } while (!mmfFound && running);
            });
        }

        public Physics ReadPhysics()
        {
            using (var stream = sharedMemory.CreateViewStream())
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

        private void ReadFunction()
        {
            try
            {
                while (running)
                {
                    // Read the Physics structure
                    var physics = ReadPhysics();

                    // Map data to controller inputs
                    controller.SetInput(0, physics.Heading * (180.0f / (float)Math.PI));    // Yaw 
                    controller.SetInput(1, physics.Pitch * (180.0f / (float)Math.PI));                      // Pitch from -0.01 to 0.01
                    controller.SetInput(2, physics.Roll * (180.0f / (float)Math.PI));                       // Roll from -0.01 to 0.01
                    controller.SetInput(3, physics.Rpms);                // RPM
                    controller.SetInput(4, physics.AccG[0]);             // Longitudinal force (x) 
                    controller.SetInput(5, physics.AccG[1]);             // Lateral force (y)
                    controller.SetInput(6, physics.AccG[2]);             // Directonal force (z)
                    controller.SetInput(7, physics.SuspensionTravel[0] * 1000); // Suspension FL
                    controller.SetInput(8, physics.SuspensionTravel[1] * 1000); // Suspension FR
                    controller.SetInput(9, physics.SuspensionTravel[2] * 1000); // Suspension BL
                    controller.SetInput(10, physics.SuspensionTravel[3] * 1000); // Suspension BR
                    controller.SetInput(11, physics.Gas); 
                    controller.SetInput(12, physics.Brake);
                    controller.SetInput(13, physics.Fuel);
                    controller.SetInput(14, physics.Gear);
                    controller.SetInput(15, physics.SteerAngle);
                    controller.SetInput(16, physics.SpeedKmh);
                    controller.SetInput(17, physics.Velocity[0]); // Longitudinal velocity (x)
                    controller.SetInput(18, physics.Velocity[1]); // Lateral velocity (y)
                    controller.SetInput(19, physics.Velocity[2]); // Directional velocity (z)
                    controller.SetInput(20, physics.WheelSlip[0]);
                    controller.SetInput(21, physics.WheelSlip[1]);
                    controller.SetInput(22, physics.WheelSlip[2]);
                    controller.SetInput(23, physics.WheelSlip[3]);
                    controller.SetInput(24, physics.WheelLoad[0]);
                    controller.SetInput(25, physics.WheelLoad[1]);
                    controller.SetInput(26, physics.WheelLoad[2]);
                    controller.SetInput(27, physics.WheelLoad[3]);
                    controller.SetInput(28, physics.WheelsPressure[0]);
                    controller.SetInput(29, physics.WheelsPressure[1]);
                    controller.SetInput(30, physics.WheelsPressure[2]);
                    controller.SetInput(31, physics.WheelsPressure[3]);
                    controller.SetInput(32, physics.WheelAngularSpeed[0]);
                    controller.SetInput(33, physics.WheelAngularSpeed[1]);
                    controller.SetInput(34, physics.WheelAngularSpeed[2]);
                    controller.SetInput(35, physics.WheelAngularSpeed[3]);
                    controller.SetInput(36, physics.TyreWear[0]);
                    controller.SetInput(37, physics.TyreWear[1]);
                    controller.SetInput(38, physics.TyreWear[2]);
                    controller.SetInput(39, physics.TyreWear[3]);
                    controller.SetInput(40, physics.TyreDirtyLevel[0]);
                    controller.SetInput(41, physics.TyreDirtyLevel[1]);
                    controller.SetInput(42, physics.TyreDirtyLevel[2]);
                    controller.SetInput(43, physics.TyreDirtyLevel[3]);
                    controller.SetInput(44, physics.TyreCoreTemperature[0]);
                    controller.SetInput(45, physics.TyreCoreTemperature[1]);
                    controller.SetInput(46, physics.TyreCoreTemperature[2]);
                    controller.SetInput(47, physics.TyreCoreTemperature[3]);
                    controller.SetInput(48, physics.CamberRad[0] * (180.0f / (float)Math.PI));
                    controller.SetInput(49, physics.CamberRad[1] * (180.0f / (float)Math.PI));
                    controller.SetInput(50, physics.CamberRad[2] * (180.0f / (float)Math.PI));
                    controller.SetInput(51, physics.CamberRad[3] * (180.0f / (float)Math.PI));
                    controller.SetInput(52, physics.Drs);
                    controller.SetInput(53, physics.TC);
                    controller.SetInput(54, physics.CgHeight);
                    controller.SetInput(55, physics.CarDamage[0]);
                    controller.SetInput(56, physics.CarDamage[1]);
                    controller.SetInput(57, physics.CarDamage[2]);
                    controller.SetInput(58, physics.CarDamage[3]);
                    controller.SetInput(59, physics.Abs);
                    controller.SetInput(60, physics.KersCharge);
                    controller.SetInput(61, physics.KersInput);
                    controller.SetInput(62, physics.AutoShifterOn);
                    controller.SetInput(63, physics.RideHeight[0]);
                    controller.SetInput(64, physics.RideHeight[1]);
                    controller.SetInput(65, physics.TurboBoost);
                    controller.SetInput(66, physics.Ballast);
                    controller.SetInput(67, physics.AirDensity);
                    controller.SetInput(68, physics.AirTemp);
                    controller.SetInput(69, physics.RoadTemp);
                    controller.SetInput(70, physics.LocalAngularVelocity[0]); // Longitudinal angular velocity (x)
                    controller.SetInput(71, physics.LocalAngularVelocity[1]); // Lateral angular velocity (y)
                    controller.SetInput(72, physics.LocalAngularVelocity[2]); // Directional angular velocity (z)
                    controller.SetInput(73, physics.EngineBrake);
                    controller.SetInput(74, physics.ErsRecoveryLevel);
                    controller.SetInput(75, physics.ErsPowerLevel);
                    controller.SetInput(76, physics.ErsHeatCharging);
                    controller.SetInput(77, physics.ErsisCharging);
                    controller.SetInput(78, physics.KersCurrentKJ);
                    controller.SetInput(79, physics.DrsEnabled);
                    controller.SetInput(80, physics.BrakeTemp[0]);
                    controller.SetInput(81, physics.BrakeTemp[1]);
                    controller.SetInput(82, physics.BrakeTemp[2]);
                    controller.SetInput(83, physics.BrakeTemp[3]);
                    controller.SetInput(84, physics.Clutch);
                    controller.SetInput(85, physics.TyreTempI[0]);
                    controller.SetInput(86, physics.TyreTempI[1]);
                    controller.SetInput(87, physics.TyreTempI[2]);
                    controller.SetInput(88, physics.TyreTempI[3]);
                    controller.SetInput(89, physics.TyreTempM[0]);
                    controller.SetInput(90, physics.TyreTempM[1]);
                    controller.SetInput(91, physics.TyreTempM[2]);
                    controller.SetInput(92, physics.TyreTempM[3]);
                    controller.SetInput(93, physics.TyreTempO[0]);
                    controller.SetInput(94, physics.TyreTempO[1]);
                    controller.SetInput(95, physics.TyreTempO[2]);
                    controller.SetInput(96, physics.TyreTempO[3]);
                    controller.SetInput(97, physics.BrakeBias);
                    controller.SetInput(98, physics.LocalVelocity[0]);
                    controller.SetInput(99, physics.LocalVelocity[1]);
                    controller.SetInput(100, physics.LocalVelocity[2]);


                    Thread.Sleep(10); // Poll at 100 Hz
                }
            }
            catch (ThreadAbortException)
            {
                // Thread was aborted
            }
            catch (Exception ex)
            {
                Interaction.MsgBox($"Error reading shared memory: {ex.Message}", MsgBoxStyle.Critical, "Error");
            }
        }

        public string[] GetInputData()
        {
            return new string[] {
                "yaw", "pitch", "roll", "rpm", "force_long", "force_lat", "force_dir", "suspen_pos_fl", "suspen_pos_fr", "suspen_pos_bl", "suspen_pos_br", "gas", "brake", "fuel", 
                "gear", "steer_angle", "speed_kmh", "velocity_long", "velocity_lat", "velocity_dir", "wheel_slip_fl", "wheel_slip_fr", "wheel_slip_bl", "wheel_slip_br",
                "wheel_load_fl", "wheel_load_fr", "wheel_load_bl", "wheel_load_br",  "wheel_pressure_fl", "wheel_pressure_fr", "wheel_pressure_bl", "wheel_pressure_br",
                "wheel_angular_speed_fl", "wheel_angular_speed_fr", "wheel_angular_speed_bl", "wheel_angular_speed_br",
                "wheel_wear_fl", "wheel_wear_fr", "wheel_wear_bl", "wheel_wear_br",
                "wheel_dirty_level_fl", "wheel_dirty_level_fr", "wheel_dirty_level_bl", "wheel_dirty_level_br",
                "wheel_core_temp_fl", "wheel_core_temp_fr", "wheel_core_temp_bl", "wheel_core_temp_br",
                "wheel_camber_fl", "wheel_camber_fr", "wheel_camber_bl", "wheel_camber_br",
                "drs_enabled", "traction_control_slip_ratio", "height", 
                "car_damage_1", "car_damage_2", "car_damage_3", "car_damage_4",
                "abs_slip_ratio", "kers_input", "auto_shifter_on",
                "ride_height_front", "ride_height_back", 
                "turbo_boost", "ballast", "air_density", "air_temp", "road_temp",
                "angular_velocity_long", "angular_velocity_lat", "angular_velocity_dir",
                "engine_brake", "ers_recovery_level", "ers_heat_charging", "ers_is_charging", "kers_current_kj", "drs_enabled",
                "brake_temp_fl", "brake_temp_fr", "brake_temp_bl", "brake_temp_br",
                "clutch", 
                "wheel_inner_temp_fl", "wheel_inner_temp_fr", "wheel_inner_temp_bl", "wheel_inner_temp_br",
                "wheel_middle_temp_fl", "wheel_middle_temp_fr", "wheel_middle_temp_bl", "wheel_middle_temp_br",
                "wheel_outer_temp_fl", "wheel_outer_temp_fr", "wheel_outer_temp_bl", "wheel_outer_temp_br",
                "brake_bias", "local_velocity_x", "local_velocity_y", "local_velocity_z",
                "immersive_suspen_pos_fl", "immersive_suspen_pos_fr", "immersive_suspen_pos_bl", "immersive_suspen_pos_br"
            };
        }

        public LedEffect DefaultLED()
        {
            return new LedEffect(
                EFFECT_TYPE.FLOW_LEFTRIGHT,
                2,
                new YawColor[] {
                    new YawColor(255, 40, 0),
                    new YawColor(80, 80, 80),
                    new YawColor(255, 100, 0),
                    new YawColor(140, 0, 255),
                },
                -20f);
        }

        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(Resources.defProfile);
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