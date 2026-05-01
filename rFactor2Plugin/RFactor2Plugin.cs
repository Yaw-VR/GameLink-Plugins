using rF2SharedMemoryNet;
using rF2SharedMemoryNet.RF2Data.Enums;
using rF2SharedMemoryNet.RF2Data.Structs;

using SharedLib;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using YawGLAPI;
//[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TelemetryConsole")]
namespace RFactor2Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "RFactor2")]
    [ExportMetadata("Version", "0.1")]
#pragma warning disable CA1416 // Validate platform compatibility
    public class RFactor2Plugin : Game, IDisposable
    {

        #region Standard Properties

        public int STEAM_ID => 365960;
        public string PROCESS_NAME => "RFactor2";
        public string AUTHOR => "Trevor Jones (Drowhunter)";

        public bool PATCH_AVAILABLE => true;

        public string Description => ResourceHelper.Description;
        public Stream Logo => ResourceHelper.Logo;
        public Stream SmallLogo => ResourceHelper.SmallLogo;
        public Stream Background => ResourceHelper.Background;

        public List<Profile_Component> DefaultProfile() => _dispatcher.JsonToComponents(ResourceHelper.DefaultProfile);

        public string[] GetInputData() => InputHelper.GetValues<RFactor2TelemetryData>(default).Keys();

        public LedEffect DefaultLED() => new(EFFECT_TYPE.KNIGHT_RIDER, 0, [YawColor.WHITE], 0);


        #endregion

        
        private Config _settings;
        private IDeviceParameters _deviceParameters;



        private Thread _readThread;
        private IMainFormDispatcher _dispatcher;
        private IProfileManager _controller;

        CancellationTokenSource _readCts;

        //private MmfTelemetry<rF2Telemetry> telemetry;
        //MappedBuffer<rF2Telemetry> telemetryBuffer;
        private RF2MemoryReader _memoryReader;

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _controller = controller;
        }


        public void Init()
        {
            _readCts = new CancellationTokenSource();

            this._settings = _dispatcher.GetConfigObject<Config>();
            _deviceParameters = _dispatcher.GetDeviceParameters();

            //telemetryBuffer = new MappedBuffer<rF2Telemetry>(MM_TELEMETRY_FILE_NAME, true /*partial*/, true /*skipUnchanged*/);

            _memoryReader = new();



            _readThread = new Thread(ReadThread);
            _readThread.Start();
        }

        private async Task<(VehicleTelemetry? telemetry, ScoringInfo? scoringInfo)> ReadAsyncTelemetry()
        {
            var telemetry = await _memoryReader.GetTelemetryAsync(_readCts.Token);
            var scoring = await _memoryReader.GetScoringAsync(_readCts.Token);

            if ((telemetry == null) || (scoring == null))
            {
                return (null, null);
            }

            
            var playerVehicle = scoring.Value.Vehicles.First(vehicle => (ControlEntity)vehicle.Control == ControlEntity.Player);
            VehicleTelemetry playerTelemetry = telemetry.Value.Vehicles.First(vehicle => vehicle.ID == playerVehicle.ID);


            return (playerTelemetry, scoring.Value.ScoringInfo);
        }


        private const float rad2Deg = 180f / MathF.PI;

        async private void ReadThread()
        {
            while (!_readCts.IsCancellationRequested)
            {
                try
                {
                    var (rf2Data, scoring) = await ReadAsyncTelemetry();

                    if (rf2Data == null)
                    {
                        continue;
                    }
                    var data = new RFactor2TelemetryData();

                    var rf = rf2Data.Value;

                    var right = rf.Orientation[0].ToVector();
                    var up = rf.Orientation[1].ToVector();
                    var forward = rf.Orientation[2].ToVector();


                    var Q = System.Numerics.Quaternion.CreateFromRotationMatrix(new Matrix4x4(
                        right.X, up.X, forward.X, 0.0f,
                        right.Y, up.Y, forward.Y, 0.0f,
                        right.Z, up.Z, forward.Z, 0.0f,
                        0.0f, 0.0f, 0.0f, 1.0f
                    ));

                    (float pitch, float yaw, float roll) = Q.ToEuler();

                    Vector3 local_velocity = rf.LocalVelocity.ToVector();
                    Vector3 local_accel = rf.LocalAcceleration.ToVector();


                    
                    var angularVelocityRad = rf.LocalRotationalSpeed.ToVector();
                    
                    data.AngularVelocity = angularVelocityRad * rad2Deg;

                    var sway = CalculateCentripetalAcceleration(local_velocity, angularVelocityRad);


                    data.Kph = local_velocity.Length() * 3.6f;
                    

                    data.Pitch = pitch;
                    data.Roll = -roll;
                    data.Yaw = yaw;

                    data.Surge = local_accel.Z / 9.81f;
                    //data.Heave = local_accel.Y / 9.81f;
                    data.Sway = sway;

                    data.Accel = local_accel;
                    data.Velocity = local_velocity;

                    data.Gear = rf.Gear;
                    data.RPM = (float)(rf.EngineRPM / rf.EngineMaxRPM);

                    data.TireFL_SusHeight = NormalizeDeflection((float)rf.Wheels[0].SuspensionDeflection);
                    data.TireFR_SusHeight = NormalizeDeflection((float)rf.Wheels[1].SuspensionDeflection);
                    data.TireRL_SusHeight = NormalizeDeflection((float)rf.Wheels[2].SuspensionDeflection);
                    data.TireRR_SusHeight = NormalizeDeflection((float)rf.Wheels[3].SuspensionDeflection);

                    var gp = (GamePhase)scoring?.GamePhase;

                    //data.OnTrack = scoring?.GamePhase ?? 0;
                    //data.IsPaused = 0;// !(new GamePhase[] { GamePhase.PausedOrHeartbeat, GamePhase.SessionOver, GamePhase.SessionStopped}).Any(_ => _ == gp) ? 1f : 0f;

                    foreach (var (i, key, value) in InputHelper.GetValues(data).WithIndex())
                    {
                        _controller.SetInput(i, value);
                    }
                }
                catch (SocketException) { }
            }

        }

        bool isRestting = false;


        CancellationTokenSource _cts = new CancellationTokenSource();

        public Dictionary<string, ParameterInfo[]> GetFeatures() => null;

        public Type GetConfigBody() => typeof(Config);

        public void Exit()
        {
            
            _cts.Cancel();
            _readCts.Cancel();
            try
            {
                _memoryReader?.Dispose();
            }
            catch { }
            //telemetry?.Dispose();
        }

       

        public void PatchGame()
        {
#if DEBUG
            Debugger.Launch();
#endif

        }


        private float CalculateCentripetalAcceleration(Vector3 velocity, Vector3 angularVelocity)
        {
            var Fc = velocity.Length() * angularVelocity.Length();

            return Fc * (angularVelocity.Y >= 0 ? -1 : 1);

        }

        private static float NormalizeDeflection(float deflectionMeters, float maxCompressionMeters = 0.15f, float maxExtensionMeters = 0.15f)
        {
            // Avoid division by zero
            if (maxCompressionMeters <= 0f) maxCompressionMeters = 0.001f;
            if (maxExtensionMeters <= 0f) maxExtensionMeters = 0.001f;

            float normalized;
            if (deflectionMeters >= 0f)
            {
                normalized = deflectionMeters / maxCompressionMeters; // compression -> positive
            }
            else
            {
                normalized = deflectionMeters / -maxExtensionMeters; // extension -> negative
            }

            // Clamp to [-1, 1]
            if (normalized > 1f) return 1f;
            if (normalized < -1f) return -1f;
            return normalized;
        }

        public void Dispose()
        {
            try
            {
                _memoryReader?.Dispose();
            }
            catch { }
        }
    }

#pragma warning restore CA1416 // Validate platform compatibility
    [StructLayout(LayoutKind.Sequential)]
    internal struct RFactor2TelemetryData
    {
        /// <summary>
        /// yaw in degrees
        /// </summary>
        public float Yaw;

        /// <summary>
        /// pitch in degrees
        /// </summary>
        public float Pitch;

        /// <summary>
        /// roll in degrees
        /// </summary>
        public float Roll;

        /// <summary>
        /// centripetal acceleration in g's
        /// </summary>
        public float Sway;

        /// <summary>
        /// logitudinal acceleration in g's
        /// </summary>
        public float Surge;

        /// <summary>
        /// lateral acceleration in g's
        /// </summary>
        public float Heave;
        /// <summary>
        /// kilometers per hour
        /// </summary>
        public float Kph;
        public float MaxKph;

        /// <summary>
        /// rpm of the engine as a fraction of the max rpm
        /// </summary>
        public float RPM;

        public float TireFL_SusHeight;
        public float TireFR_SusHeight;
        public float TireRL_SusHeight;
        public float TireRR_SusHeight;

        //public float OnTrack;
        //public float IsPaused;
        //public float Loading;

        //public Quat Rot;

        /// <summary>
        /// Angular velocity in degrees per second where x = pitch rate, y = yaw rate, z = roll rate
        /// </summary>
        internal Vector3 AngularVelocity;

        /// <summary>
        /// acceleration in m/s² where x = lateral, y = vertical, z = longitudinal
        /// </summary>
        internal Vector3 Accel;

        /// <summary>
        /// velocity in m/s where x = lateral, y = vertical, z = longitudinal
        /// </summary>
        internal Vector3 Velocity;
        internal int Gear;
    }

    internal struct Quat
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    internal static class RFactor2TelemetryDataExtensions
    {
        public static Vector3 ToVector(this Vec3 vec)
        {
            return new Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
        }

        
    }
}