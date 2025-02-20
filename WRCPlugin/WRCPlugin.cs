using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using WRCPlugin.Properties;
using YawGLAPI;
namespace WRCPlugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "EA Sports WRC")]
    [ExportMetadata("Version", "1.0")]
    public class WRCPlugin : Game
    {
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private bool running;
        private UdpClient receivingUdpClient;
        private Thread readThread;

        public int STEAM_ID => 1849250;

        public string PROCESS_NAME => "WRC";

        public bool PATCH_AVAILABLE => true;

        public string AUTHOR => "YawVR";

        public Stream Logo => GetStream("logo.png");

        public Stream SmallLogo => GetStream("recent.png");

        public Stream Background => GetStream("wide.png");

        public string Description => Resources.description;

        public LedEffect DefaultLED()
        {
            return new LedEffect();
        }

        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(Resources.profile);
        }

        public void Exit()
        {
            running = false;
            receivingUdpClient.Close();
            receivingUdpClient.Dispose();
            receivingUdpClient = null;
        }

        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return new Dictionary<string, ParameterInfo[]>();
        }

        public string[] GetInputData()
        {
            Type t = typeof(PluginOutput);
            FieldInfo[] fields = t.GetFields();

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
            running = true;
            receivingUdpClient = new UdpClient(pConfig.Port);
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }

        private void ReadFunction()
        {
            WRCTelemetryData sende = new WRCTelemetryData();
            FieldInfo[] fields = typeof(PluginOutput).GetFields();
            IPEndPoint senderIP = null;

            PluginOutput output = new PluginOutput();
            while (running)
            {
                try
                {
                    byte[] rawData = receivingUdpClient.Receive(ref senderIP);



                    IntPtr unmanagedPointer =
                    Marshal.AllocHGlobal(rawData.Length);
                    Marshal.Copy(rawData, 0, unmanagedPointer, rawData.Length);
                    // Call unmanaged code
                    Marshal.FreeHGlobal(unmanagedPointer);
                    Marshal.PtrToStructure(unmanagedPointer, sende);


                    output.brake = sende.vehicle_brake;
                    output.throttle = sende.vehicle_throttle;
                    output.rpm = sende.vehicle_engine_rpm_current / sende.vehicle_engine_rpm_max;
                    output.speed = sende.vehicle_speed;
                    output.handbrake = sende.vehicle_handbrake;

                    // Create the rotation matrix
                    float[,] R = new float[3, 3]
                    {
            { sende.vehicle_forward_direction_x, sende.vehicle_left_direction_x, sende.vehicle_up_direction_x },
            { sende.vehicle_forward_direction_y, sende.vehicle_left_direction_y, sende.vehicle_up_direction_y },
            { sende.vehicle_forward_direction_z, sende.vehicle_left_direction_z, sende.vehicle_up_direction_z }
                    };

                    // Transpose the rotation matrix
                    float[,] RT = new float[3, 3]
                    {
            { R[0, 0], R[1, 0], R[2, 0] },
            { R[0, 1], R[1, 1], R[2, 1] },
            { R[0, 2], R[1, 2], R[2, 2] }
                    };

                    // Global acceleration vector
                    float[] aGlobal = new float[3]
                    {
            sende.vehicle_acceleration_x,
            sende.vehicle_acceleration_y,
            sende.vehicle_acceleration_z
                    };

                    // Compute local acceleration vector
                    output.local_acc_x = RT[0, 0] * aGlobal[0] + RT[0, 1] * aGlobal[1] + RT[0, 2] * aGlobal[2];
                    output.local_acc_y = RT[1, 0] * aGlobal[0] + RT[1, 1] * aGlobal[1] + RT[1, 2] * aGlobal[2];
                    output.local_acc_z = RT[2, 0] * aGlobal[0] + RT[2, 1] * aGlobal[1] + RT[2, 2] * aGlobal[2];



                    var (yaw, pitch, roll) = CalculateYawPitchRoll(sende);
                    output.yaw = (float)yaw;
                    output.pitch= (float)pitch;
                    output.roll = (float)roll;
                    

                    for (int i = 0; i < fields.Length; i++)
                    {
                        controller.SetInput(i, (float)fields[i].GetValue(output));
                    }
                }
                catch (SocketException) { }
            }
        }

        public void PatchGame()
        {
            string fPath = @Environment.GetEnvironmentVariable("userprofile") + "\\Documents\\My Games\\WRC\\telemetry\\config.json";
            if (!File.Exists(fPath))
            {
                dispatcher.ShowNotification(NotificationType.ERROR,"Config file not found! Please make sure to run the game at least once before patching!");
                return;
            }

            JToken f = JToken.Parse(File.ReadAllText(fPath));
            JArray packets = (JArray)f["udp"]["packets"];

            JToken customtoken = packets.Where(p => p["structure"].ToString() == "custom1").First();
            customtoken["bEnabled"] = true;

            File.WriteAllText(fPath, JsonConvert.SerializeObject(f,Formatting.Indented));
            return;
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }


        public static (double yaw, double pitch, double roll) CalculateYawPitchRoll(WRCTelemetryData telemetryData)
        {
            // Calculate yaw (rotation around Z-axis)
            double yaw = Math.Atan2(telemetryData.vehicle_forward_direction_x, telemetryData.vehicle_forward_direction_z);

            // Calculate pitch (rotation around Y-axis)
            double pitch = Math.Asin(telemetryData.vehicle_forward_direction_y);

            double roll = Math.Asin(telemetryData.vehicle_left_direction_y);

            // Convert radians to degrees
            double yawDegrees = yaw * (180.0 / Math.PI);
            double pitchDegrees = pitch * (180.0 / Math.PI);
            double rollDegrees = roll * (180.0 / Math.PI);

            return (yawDegrees, pitchDegrees, rollDegrees);
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
