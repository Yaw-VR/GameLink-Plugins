using F12024Plugin.Properties;
using F1Sharp;
using System.ComponentModel.Composition;
using System.Reflection;
using YawGLAPI;
namespace F12024Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "F1 2024")]
    [ExportMetadata("Version", "1.0")]
    public class F12024Plugin : Game
    {
        private struct F12024TelemetryData
        {
            public float speed;
            public float throttle;
            public float steer;
            public float brake;
            public float clutch;
            public float gear;
            public float engineRPM;
            public float drs;
            public float revLightsPercent;

            public float gForceLateral;
            public float gForceLongitudinal;
            public float gForceVertical;
            public float yaw;
            public float pitch;
            public float roll;
            public float wheelSlip;
            public float onAsphalt;

        }
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private TelemetryClient client;
        private F12024TelemetryData data;

        FieldInfo[] fields = typeof(F12024TelemetryData).GetFields();
        public int STEAM_ID => 2488620;

        public string PROCESS_NAME => "F1_24";

        public bool PATCH_AVAILABLE => false;

        public string AUTHOR => "YawVR";

        public Stream Logo => GetStream("f1logo.png");

        public Stream SmallLogo => GetStream("f1small.png");

        public Stream Background => GetStream("f1wide.png");

        public string Description => Resources.description;

        public LedEffect DefaultLED()
        {
            return new LedEffect();
        }

        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit()
        {
            client.Stop();
        }

        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return null;
        }

        public string[] GetInputData()
        {
            string[] inputs = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                inputs[i] = fields[i].Name;
            }
            return inputs;
        }

        public void Init()
        {

            var pConfig = dispatcher.GetConfigObject<Config>();
            client = new TelemetryClient(pConfig.Port);

            client.OnCarTelemetryDataReceive += Client_OnCarTelemetryDataReceive;
            client.OnMotionDataReceive += Client_OnMotionDataReceive;
            client.OnMotionExDataReceive += Client_OnMotionExDataReceive;
        }


        private void Client_OnMotionExDataReceive(F1Sharp.Packets.MotionExPacket packet)
        {
            data.wheelSlip = packet.wheelSlipRatio.Average();
        }

        private void Client_OnMotionDataReceive(F1Sharp.Packets.MotionPacket packet)
        {
            int pIndex = packet.header.playerCarIndex;
            data.yaw = packet.carMotionData[pIndex].yaw;
            data.pitch = packet.carMotionData[pIndex].pitch;
            data.roll = packet.carMotionData[pIndex].roll;

            data.gForceLateral = packet.carMotionData[pIndex].gForceLateral;
            data.gForceLongitudinal = packet.carMotionData[pIndex].gForceLongitudinal;
            data.gForceVertical = packet.carMotionData[pIndex].gForceVertical;




            // Call unmanaged code


            for (int i = 0; i < fields.Length; i++)
            {
                controller.SetInput(i, (float)fields[i].GetValue(data));
            }
        }

        private void Client_OnCarTelemetryDataReceive(F1Sharp.Packets.CarTelemetryPacket packet)
        {
            int pIndex = packet.header.playerCarIndex;

            data.speed = packet.carTelemetryData[pIndex].speed;
            data.throttle = packet.carTelemetryData[pIndex].throttle;
            data.steer = packet.carTelemetryData[pIndex].steer;
            data.brake = packet.carTelemetryData[pIndex].brake;
            data.clutch = packet.carTelemetryData[pIndex].clutch;

            data.gear = packet.carTelemetryData[pIndex].gear;
            data.engineRPM = packet.carTelemetryData[pIndex].engineRPM;
            data.drs = packet.carTelemetryData[pIndex].drs;
            data.revLightsPercent = packet.carTelemetryData[pIndex].revLightsPercent;

            data.onAsphalt = packet.carTelemetryData[pIndex].surfaceType.All(x => x == SurfaceType.TARMAC) ? 1 : 0;
        }

        public void PatchGame()
        {
            return;
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
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
