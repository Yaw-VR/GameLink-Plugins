using SharedLib;
using SharedLib.TelemetryHelper;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

using YawGLAPI;

namespace KayakVRMiragePlugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Kayak VR: Mirage")]
    [ExportMetadata("Version", "1.0")]
    public class KayakVRMiragePlugin : Game
    {
        #region Standard Properties

        public int STEAM_ID => 1683340;
        public string PROCESS_NAME => "KayakVR-Win64-Shipping";
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "YawVR";

        public string Description => ResourceHelper.Description;
        public Stream Logo => ResourceHelper.Logo;
        public Stream SmallLogo => ResourceHelper.SmallLogo;
        public Stream Background => ResourceHelper.Background;

        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(ResourceHelper.DefaultProfile);

        public string[] GetInputData() => InputHelper.GetValues<SRSPacket>(default).Keys();

        public LedEffect DefaultLED() => new(EFFECT_TYPE.KNIGHT_RIDER, 0, [YawColor.WHITE], 0);

        #endregion

        private Config settings;
        private UdpTelemetry<SRSPacket> telemetry;
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
            settings = dispatcher.GetConfigObject<Config>();
            running = true;
            readThread = new Thread(new ThreadStart(ReadThread));
            readThread.IsBackground = true;
            readThread.Start();
        }

        private void ReadThread()
        {
            try
            {
                telemetry = new UdpTelemetry<SRSPacket>(new UdpTelemetryConfig
                {
                    ReceiveAddress = new IPEndPoint(IPAddress.Any, settings.Port)
                }, new SRSPacketConverter());
            }
            catch (Exception x)
            {
                dispatcher.ShowNotification(NotificationType.ERROR, x.Message);
                Exit();
                return;
            }

            while (running)
            {
                try
                {
                    var data = telemetry.Receive();

                    foreach (var (i, _, value) in InputHelper.GetValues(data).WithIndex())
                    {
                        controller.SetInput(i, value);
                    }
                }
                catch (SocketException) { }
            }
        }

        public void Exit()
        {
            running = false;
            telemetry?.Dispose();
        }

        public void PatchGame() { }

        public Dictionary<string, ParameterInfo[]> GetFeatures() => null;

        public Type GetConfigBody() => typeof(Config);
    }
}
