using SharedLib;
using SharedLib.TelemetryHelper;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Distance")]
    [ExportMetadata("Version", "1.6")]

    public class DistancePlugin : Game
    {

        #region Standard Properties

        public int STEAM_ID => 233610;
        public string PROCESS_NAME => "Distance";
        public string AUTHOR => "Trevor Jones (Drowhunter)";

        public bool PATCH_AVAILABLE => true;

        public string Description => ResourceHelper.Description;
        public Stream Logo => ResourceHelper.Logo;
        public Stream SmallLogo => ResourceHelper.SmallLogo;
        public Stream Background => ResourceHelper.Background;

        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(ResourceHelper.DefaultProfile);

        public string[] GetInputData() => InputHelper.GetValues<DistanceTelemetryData>(default).Keys();

        public LedEffect DefaultLED() => new(EFFECT_TYPE.KNIGHT_RIDER, 0, [YawColor.WHITE], 0);


        #endregion
        private Config settings;
        private IDeviceParameters deviceParameters;


        private UdpTelemetry<DistanceTelemetryData> telemetry;
        private Thread readThread;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;


        private volatile bool running = false;



        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }


        public void Init()
        {
            this.settings = dispatcher.GetConfigObject<Config>();
            deviceParameters = dispatcher.GetDeviceParameters();
            running = true;
            readThread = new Thread(new ThreadStart(ReadThread));
            readThread.Start();
        }

        private void ReadThread()
        {
            try
            {
                telemetry = new UdpTelemetry<DistanceTelemetryData>(new UdpTelemetryConfig
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
                    var data = telemetry.Receive();

                    if (data.IsCarIsActive)
                    {
                        if (!isRestting)
                        {
                            foreach (var (i, key, value) in InputHelper.GetValues(data).WithIndex())
                            {
                                var nv = 0f;

                                switch (key)
                                {
                                    case nameof(DistanceTelemetryData.Pitch):
                                        nv = MathsF.ScalePitchRoll(value, -90, 90, -deviceParameters.PitchLimitF, deviceParameters.PitchLimitB);
                                        break;
                                    case nameof(DistanceTelemetryData.Roll):
                                        nv = MathsF.ScalePitchRoll(value, -90, 90, -deviceParameters.RollLimit, deviceParameters.RollLimit);
                                        break;
                                    default:
                                        nv = value;
                                        break;
                                }

                                controller.SetInput(i, nv);
                            }
                        }
                        else
                        {
                            isRestting = false;
                            Thread.Sleep(1000);
                        }
                    }
                    else if (!isRestting)
                    {
                        isRestting = true;
                    }
                }
                catch (SocketException) { }
            }

        }

        bool isRestting = false;


        CancellationTokenSource cts = new CancellationTokenSource();

        public Dictionary<string, ParameterInfo[]> GetFeatures() => null;

        public Type GetConfigBody() => typeof(Config);

        public void Exit()
        {
            running = false;
            cts.Cancel();
            telemetry?.Dispose();
        }

       

        public async void PatchGame()
        {
#if DEBUG
            Debugger.Launch();
#endif

            await UnityPatcher.Create<UnityPatcher>(this, dispatcher, options =>
            {
                //options.GameInstallPath = installPath;
                options.ModType = ModType.BepInEx5_x64;
                options.PluginName = "DistanceTelemetryMod";
                options.DoorStopPath = "";
                options.Repository = new GithubOptions
                {
                    UsernameOrOrganization = "Unity-Telemetry-Mods",
                    Repository = "Distance-TelemetryMod"
                };                
            }).PatchAsync(cts.Token);
            
            
           
        }

    }

    

}