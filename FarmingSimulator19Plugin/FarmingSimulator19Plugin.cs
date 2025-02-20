using FarmingSimulator19Plugin.Properties;
using FarmingSimulatorSDKClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Reflection;
using YawGLAPI;

namespace YawVR_Game_Engine
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Farming Simulator 19")]
    [ExportMetadata("Version", "1.0")]

    public class F12020Plugin : Game {
        private FSTelemetryReader telemetryReader;


        private float prev;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;

        public string PROCESS_NAME => "FarmingSimulator2019Game";
        public bool PATCH_AVAILABLE => true;
        public string AUTHOR => "YawVR";
        private readonly string LUA_DL_LINK = "https://yaw.one/gameengine/Plugins/Farming_Simulator_19/TelemetriaFarmingSimulator.zip";

        public int STEAM_ID => 787860;

        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        public LedEffect DefaultLED() {
            return dispatcher.JsonToLED(Resources.defProfile);
        }

        public List<Profile_Component> DefaultProfile() {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }

        public void Exit() {
            telemetryReader?.Stop();

        }

        public string[] GetInputData() {
            string[] inputs = new string[] {
                "Heading","Speed","AngularVel","RPM"
            };
            return inputs;
        }

      
        public void SetReferences(IProfileManager controller,IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init() {
            telemetryReader = new FSTelemetryReader();
            telemetryReader.OnTelemetryRead += TelemetryReader_OnTelemetryRead;
            telemetryReader.Start();

        }

        private void TelemetryReader_OnTelemetryRead(FSTelemetry telemetry) {
            float heading = NormalizeAngle((float)telemetry.AngleRotation);
            controller.SetInput(0,  heading);
            controller.SetInput(1, (float)telemetry.Speed);
            controller.SetInput(2, (float)CalculateDifferenceBetweenAngles(prev, heading));
            float rpm = telemetry.RPMMax == 0 ? 0 : (float)telemetry.RPM / telemetry.RPMMax;
            controller.SetInput(3, rpm);
            prev = heading;
        }

        public void PatchGame() {
            var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games");
            
           


            string modsPath = defaultPath + @"\FarmingSimulator2019\mods";
            Console.WriteLine("Mods folder is " + modsPath, DIALOG_TYPE.INFO);

            Directory.CreateDirectory(modsPath);

            using(WebClient wc = new WebClient()) {
                wc.DownloadFile(LUA_DL_LINK, modsPath + @"/TelemetriaFarmingSimulator.zip");
            }

            dispatcher.ShowNotification(NotificationType.INFO, "FS2019 patched!");

        }
      
        public static float NormalizeAngle(float angle)
        {
            float newAngle = angle;
            while (newAngle <= -180) newAngle += 360;
            while (newAngle > 180) newAngle -= 360;
            return newAngle;
        }

        public static double CalculateDifferenceBetweenAngles(double firstAngle, double secondAngle)
        {
            double difference = secondAngle - firstAngle;
            while (difference < -180) difference += 360;
            while (difference > 180) difference -= 360;
            return difference;
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
