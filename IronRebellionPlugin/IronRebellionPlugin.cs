using IronRebellion;

using SharedLib;
using SharedLib.TelemetryHelper;

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;

using YawGLAPI;


namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Iron Rebellion")]
    [ExportMetadata("Version", "0.14")]
    public class Plugin : Game
    {

        #region Standard Properties
        public int STEAM_ID => 1192900; // Game's SteamID. App will lauch game based on this
        public string PROCESS_NAME => "Iron Rebellion"; // The gameprocess name. App will wait/monitor this process for different features like autostart.

        public bool PATCH_AVAILABLE => true; // Tell app if patch is needed. "Patch" Button will appear -> Needed manually

        public string AUTHOR => "McFredward"; // Creator, will show this on plugin manager

        public string Description => ResourceHelper.Description;
        public Stream Logo => ResourceHelper.Logo;
        public Stream SmallLogo => ResourceHelper.SmallLogo;
        public Stream Background => ResourceHelper.Background;
        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(ResourceHelper.DefaultProfile);

        public LedEffect DefaultLED() => dispatcher.JsonToLED(ResourceHelper.DefaultProfile);

        private IMainFormDispatcher dispatcher; // this is our reference to the app. Features like showing dialog/notification can be used
        private IProfileManager controller; // this is our reference to profile manager. input values need to be passed to this

        /// <summary>
        /// The app will give us these references. We need to save them
        /// </summary>
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }

        #endregion


        private UdpTelemetry<TelemetryData> telemetry;

        IPEndPoint remoteEndPoint;

        private bool running = false;

        private CancellationTokenSource tokenSource;

        private Config settings;

        /// <summary>
        /// Will be called at plugin stop request
        /// </summary>
        public void Exit()
        {
            tokenSource?.Cancel();
            telemetry.Dispose();           
            running = false;
        }

        /// <summary>
        /// App fetches available inputs through this
        /// </summary>
        /// <returns></returns>
        public string[] GetInputData() => GetInputs<TelemetryData>(default).Select(_ => _.key).ToArray();


        /// <summary>
        /// Features for the plugin. 
        /// These features can be setup to be called for different events like pushing arcade buttons
        /// </summary>
        public Dictionary<string, ParameterInfo[]> GetFeatures() => null;

        public Type GetConfigBody() => typeof(Config);

        /// <summary>
        /// Will be called when app starts this plugin
        /// </summary>
        public void Init()
        {

            
            tokenSource = new();
            this.settings = dispatcher.GetConfigObject<Config>();
            running = true;

            new Thread(ReadThread).Start();
        }


        public void ReadThread()
        {

            int telemetryDataSize = Marshal.SizeOf(typeof(TelemetryData));
            try
            {
                telemetry = new UdpTelemetry<TelemetryData>(new UdpTelemetryConfig
                {
                    ReceiveAddress = new IPEndPoint(IPAddress.Parse(settings.IP), settings.Port)
                });

            }
            catch (Exception x)
            {
                dispatcher.ShowNotification(NotificationType.ERROR, x.Message);
                Exit();
            }

            while (running)
            {
                try
                {
                    // Receive UDP data
                    var telem = telemetry.Receive();
                    
                    foreach (var (i, key, value) in GetInputs(telem).WithIndex())
                    {
                        controller.SetInput(i, value);
                    }
                   
                }
                catch (SocketException ex)
                {
                    Debug.WriteLine($"Socket error: {ex.Message}");
                    break; // Exit loop on socket error
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }

            }

            
        }

        public async void PatchGame()
        {
            

            var patcher = UnityPatcher.Create<UnityPatcher>(this, dispatcher, options =>
            {
                options.ModType = ModType.BepInEx5_x64;
                options.PluginName = "IronRebellionTelemetry";
                options.DoorStopPath = "";
                options.Repository = new GithubOptions
                {
                    UsernameOrOrganization = "McFredward",
                    Repository = "IronRebellionTelemetry"
                };
            });

            CancellationTokenSource tk = new (TimeSpan.FromSeconds(600));

#if DEBUG
            Debugger.Launch();
#endif
            await patcher.PatchAsync(tk.Token);



        }


        private static IEnumerable<(string key, float value)> GetInputs<T>(T data)
        {
            foreach (var field in (data?.GetType() ?? typeof(T)).GetFields())
            {
                if (field.FieldType.IsPrimitive)
                    yield return (field.Name, GetFloat(field, data));
                else
                    foreach (var (k, v) in GetInputs(field.GetValue(data)))
                        yield return (field.Name + "." + k, v);
            }

            float GetFloat(FieldInfo f, object? data = null)
            {
                var retval = data == null ? 0 : (float)Convert.ChangeType(f.GetValue(data), typeof(float));
                return retval;
            }
        }



    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TelemetryData
    {
        public float velocityX;
        public float velocityY;
        public float velocityZ;

        public float angularX;
        public float angularY;
        public float angularZ;

        public float rotationX;
        public float rotationY;
        public float rotationZ;

        public float adjustedTilt;
        public float currentLean;
        

        public bool isFlying;
        public bool isRunning;
        public bool isHit;
        public bool weaponFired;
        public bool stomped;
        public bool landed;
        public bool jumped;

        // 0 = none, -1 = left, 1 = right
        public float stompedFoot;

        public float speed;

        public TelemetryData()
        {
        }
    }

}
