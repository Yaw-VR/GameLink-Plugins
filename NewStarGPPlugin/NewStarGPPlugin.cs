using SharedLib;
using SharedLib.TelemetryHelper;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "New Star GP")]
    [ExportMetadata("Version", "1.0")]
    public class NewStarGPPlugin : Game
    {
        

        #region Standard Properties
        public int STEAM_ID => 2217580;
        public string PROCESS_NAME => "NSGP";
        public string AUTHOR => "Trevor Jones (Drowhunter)";

        public bool PATCH_AVAILABLE => true;

        
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

        public string[] GetInputData() => InputHelper.GetValues<Telemetry>(default).Keys();


        private volatile bool running = false;
        private UdpTelemetry<Telemetry> telem;
        private Thread readThread;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        CancellationTokenSource cts = new();

        public void Exit()
        {
            running = false;
            cts.Cancel();
            telem?.Dispose();
        }

       
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

        public void Init()
        {
            this.settings = dispatcher.GetConfigObject<Config>();
           
            running = true;
            readThread = new Thread(new ThreadStart(ReadThread));
            readThread.Start();
        }

        private void ReadThread()
        {
            
            telem = new UdpTelemetry<Telemetry>(new UdpTelemetryConfig
            {
                ReceiveAddress = new IPEndPoint(IPAddress.Parse(settings.IP), settings.Port)
            });           
            

            while (running)
            {
                try
                {
                    foreach (var (i, key, value) in InputHelper.GetValues(telem.Receive()).WithIndex())
                        controller.SetInput(i, value);
                }
                catch(SocketException) { }
            }
            
        }

       
        

        public async void PatchGame()
        {
#if DEBUG
            Debugger.Launch();
#endif

            bool UseUUVR = false;
            dispatcher.DialogShow("Use UUVR with RaiPal?", DIALOG_TYPE.QUESTION, (yes) => UseUUVR = true);

            var patcher = UnityPatcher.Create<UnityPatcher>(this, dispatcher, options =>
            {
                options.ModType = UseUUVR ? ModType.RaiPal : ModType.BepInEx5_x64;
                options.PluginName = "NewStarGPTelemetryMod";
                options.DoorStopPath = "release";
                options.Repository = new GithubOptions
                {
                    UsernameOrOrganization = "Unity-Telemetry-Mods",
                    Repository = "NewStarGP-TelemetryMod"
                };
            });


            await patcher.PatchAsync(cts.Token);            
        }

    }


}