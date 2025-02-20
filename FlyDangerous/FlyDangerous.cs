using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using YawGLAPI;

namespace FlyDangerous
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Fly Dangerous")]
    [ExportMetadata("Version", "1.0")]

    public class FlyDangerous : Game
    {
        public int STEAM_ID => 1781750;
        public string PROCESS_NAME => "FlyDangerous";
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "DannyDan";
        public Stream Logo => GetStream("logo.jpg");
        public Stream SmallLogo => GetStream("logo_small.jpg");
        public Stream Background => GetStream("logo.jpg");
        public string Description => "Go to 'Options -> Integrations -> enable 'Raw Telemetry' and set 'Telemetry Mode' to 'BYTES'.";

        private volatile bool running = false;
        private IProfileManager controller;
        private Thread readThread;
        private UdpClient receivingUdpClient;
        private IMainFormDispatcher dispatcher;
        private IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private FieldInfo[] fields;

        public LedEffect DefaultLED() {
            return dispatcher.JsonToLED(GetString("default.yawglprofile"));
        }

        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(GetString("default.yawglprofile"));
        }

        public void Exit()
        {
            receivingUdpClient.Close();
            receivingUdpClient = null;
            running = false;
        }
        private bool IsValidType(Type t)
        {
            return t == typeof(float) || t == typeof(bool);
        }

        public string[] GetInputData()
        {
            Type t = typeof(FlyDangerousTelemetry);

            fields = t.GetFields().Where(field => IsValidType(field.FieldType)).ToArray();

            string[] inputs = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                inputs[i] = fields[i].Name;
            }
            return inputs;

        }

        public void Init()
        {
            running = true;
            receivingUdpClient = new UdpClient(11000);
            receivingUdpClient.Client.ReceiveTimeout = 2000; // 2sec timeout
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }
        private float NormalizeAngle(float value)
        {
            float ret = value;
            if (ret > 180)
                ret = (ret - 360);

            return ret;
        }
        private void ReadFunction()
        {

            while (running)
            {
                try
                {
                    var buffer = receivingUdpClient.Receive(ref remoteIpEndPoint);
                    var size = Marshal.SizeOf(telemetry);
                    var ptr = IntPtr.Zero;
                    try
                    {
                        ptr = Marshal.AllocHGlobal(size);
                        Marshal.Copy(buffer, 0, ptr, size);
                        telemetry = (FlyDangerousTelemetry)Marshal.PtrToStructure(ptr, telemetry.GetType());
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }


                    telemetry.shipWorldRotationEulerX = NormalizeAngle(telemetry.shipWorldRotationEulerX);
                    telemetry.shipWorldRotationEulerY = NormalizeAngle(telemetry.shipWorldRotationEulerY);
                    telemetry.shipWorldRotationEulerZ = NormalizeAngle(telemetry.shipWorldRotationEulerZ);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        controller.SetInput(i, (float)Convert.ChangeType(fields[i].GetValue(telemetry), TypeCode.Single));
                    }
                }
                catch (SocketException) { }
                catch (ObjectDisposedException)
                {
                    running = false;
                    break;
                }
               
            }


        }

        private static FlyDangerousTelemetry telemetry;

        public void PatchGame()
        {
            return;
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
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
        private string GetString(string resourceName)
        {

            var result = string.Empty;
            try
            {
                using var stream = GetStream(resourceName);

                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    result = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                dispatcher.ShowNotification(NotificationType.ERROR, "Error loading resource - " + e.Message);
            }


            return result;
        }

        public Type GetConfigBody()
        {
            return null;
        }
    }
  

  

}

