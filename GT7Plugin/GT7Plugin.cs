using Microsoft.VisualBasic;

using PluginHelper;

using SharedLib;

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;

using YawGLAPI;

using Quaternion = System.Numerics.Quaternion;

namespace GT7Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Gran Turismo 7")]
    [ExportMetadata("Version", "1.4")]
    public class GT7Plugin : Game
    {
        #region Standard Properties

        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;


        private Stopwatch suspStopwatch = new Stopwatch();
        public int STEAM_ID => 0;

        public string PROCESS_NAME => string.Empty;

        public bool PATCH_AVAILABLE => false;

        public string AUTHOR => "Trevor Jones";

        public string Description => ResourceHelper.Description;
        public Stream Logo => ResourceHelper.Logo;
        public Stream SmallLogo => ResourceHelper.SmallLogo;
        public Stream Background => ResourceHelper.Background;

        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(ResourceHelper.DefaultProfile);
        public LedEffect DefaultLED() => new LedEffect(EFFECT_TYPE.KNIGHT_RIDER, 0, [YawColor.WHITE], 0);
        public Dictionary<string, ParameterInfo[]> GetFeatures() => null;

        public Type GetConfigBody() => typeof(Config);

        private Config settings;

        #endregion

        private UDPListener listener;
        
        public void Exit() => listener?.Stop();

        public string[] GetInputData() => InputHelper.GetValues<GT7Output>(default).Keys();

        

        public void Init()
        {
            this.settings = dispatcher.GetConfigObject<Config>();
            listener = new UDPListener(SimInterfacePacketType.PacketType2, settings.Port);
            listener.OnPacketReceived += Listener_OnPacketReceived;
            suspStopwatch.Restart();
        }

        private void Listener_OnPacketReceived(object sender, byte[] buffer)
        {
            var sp = new SimulatorPacket();
            sp.Read(buffer);

            // after race finishes game still sends telemetry, checking the laps to detect when actually racing  
            bool preRace = sp.LapsInRace == 0 && sp.LapCount == 0;
            bool raceFinished = sp.LapCount > sp.LapsInRace;
            bool inRace = !preRace && !raceFinished;
            
            if (inRace && sp.Flags.HasFlag(SimulatorFlags.CarOnTrack) && !sp.Flags.HasFlag(SimulatorFlags.Paused) && !sp.Flags.HasFlag(SimulatorFlags.LoadingOrProcessing))
            {
                ReadFunction(sp);
            }
        }

        private void ReadFunction(SimulatorPacket sp)
        {
           
            var (pitch, yaw, roll) = sp.Rotation.ToPitchYawRoll();

            bool updateSusp = false;

            if (suspStopwatch.ElapsedMilliseconds > 100)
            {
                updateSusp = true;
                suspStopwatch.Restart();
            }

            var output = new GT7Output()
            {
                Yaw = yaw,
                Pitch = -pitch,
                Roll = roll,
                Sway = sp.Sway ?? 0f,
                Surge = -sp.Surge ?? 0f,
                Heave = sp.Heave ?? 0f,
                Kph = sp.MetersPerSecond * 3.6f,
                MaxKph = sp.CalculatedMaxSpeed * 3.6f,
                RPM = sp.EngineRPM,                
                OnTrack = sp.Flags.HasFlag(SimulatorFlags.CarOnTrack) ? 1 : 0,
                IsPaused = sp.Flags.HasFlag(SimulatorFlags.Paused) ? 1 : 0,
                Loading = sp.Flags.HasFlag(SimulatorFlags.LoadingOrProcessing) ? 1 : 0,
                InRace = sp.LapsInRace > 0 ? 1 : 0,
                CentripetalAcceleration = CalculateCentripetalAcceleration(Maths.WorldtoLocal(sp.Rotation, sp.Velocity), sp.AngularVelocity)
            };

            if (updateSusp)
            {
                output.TireFL_SusHeight = sp.TireFL_SusHeight;
                output.TireFR_SusHeight = sp.TireFR_SusHeight;
                output.TireRL_SusHeight = sp.TireRL_SusHeight;
                output.TireRR_SusHeight = sp.TireRR_SusHeight;
            }

            
            foreach (var (i, key, value) in InputHelper.GetValues(output).WithIndex())
                controller.SetInput(i, value);

        }

        public void PatchGame()
        {
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }


        public float CalculateCentripetalAcceleration(Vector3 velocity, Vector3 angularVelocity)
        {
            var Fc = velocity.Length() * angularVelocity.Length();

            return Fc * (angularVelocity.Y >= 0 ? -1 : 1);

        }

       
    }
}
