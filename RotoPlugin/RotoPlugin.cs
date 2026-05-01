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
using System.Threading.Tasks;

using YawGLAPI;

namespace RotoVRPlugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "RotoVR")]
    [ExportMetadata("Version", "1.0")]

    public class RotoVRPlugin : Game {  

        
        private Thread readThread;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        
        public int STEAM_ID => 0;
        public string PROCESS_NAME => null;
        public string AUTHOR => "Drowhunter";

        public bool PATCH_AVAILABLE => true;

        public string Description => "<html><body><h1>Test Roto</h1></body></html>";// ResourceHelper.Description;

        public Stream Logo => ResourceHelper.Logo;
        public Stream SmallLogo => ResourceHelper.SmallLogo;
        public Stream Background => ResourceHelper.Background;

        CancellationTokenSource cts;


        public void Exit() {
            cts.Cancel();
        }
        public void PatchGame()
        {           
            //launch the debugger so I can debug

            Debugger.Launch();

        }

        public string[] GetInputData() => InputHelper.GetValues<Telemetry>(default).Keys();




        public LedEffect DefaultLED() => dispatcher.JsonToLED(ResourceHelper.DefaultProfile);

        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(ResourceHelper.DefaultProfile);


        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
#if USE_MMF
        MmfTelemetry<Telemetry> telem = null;
#else
        UdpTelemetry<Telemetry> telem = null;
#endif

        public async void Init()
        {
            if (!cts?.IsCancellationRequested ?? false)
            {
                cts.Cancel();
                Thread.Sleep(10); // wait for the thread to finish
            }

            if (telem != null)
            {
                telem.Dispose();
                Thread.Sleep(10); // wait for the thread to finish
            }
            

            cts = new CancellationTokenSource();
#if USE_MMF
            telem = new MmfTelemetry<Telemetry>(config: new MmfTelemetryConfig { Name = "RotoVR" });
#else
            telem = new UdpTelemetry<Packet>(new UdpTelemetryConfig { ReceiveAddress = new IPEndPoint(IPAddress.Any, 16969)   });
#endif

            readThread = new Thread(async () =>
            {
                try
                {
                    await ReadThreadAsync(cts.Token);
                }
                catch (Exception e)
                {
                    //show error message
                    //dispatcher.ShowNotification(NotificationType.ERROR, "Error starting telemetry thread: " + e.Message);
                }
            })  { Name = "RotoVRPlugin", IsBackground = true };

           

            readThread.Start();
            
        }

        private async Task ReadThreadAsync(CancellationToken cancellationToken) {

#if USE_MMF
            int a = await telem.TryOpenAsync(0, cancellationToken);
#else
            int a = 0;
#endif
            if (a > 0)
            {
                //error opening the telemetry
                //show error message
                dispatcher.ShowNotification(NotificationType.ERROR, "Error opening telemetry");
                return;
            }

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var data = await telem.ReceiveAsync(cts.Token);
                    foreach (var (i, key, value) in InputHelper.GetValues(data).WithIndex())
                        controller.SetInput(i, value);
                }
                catch (Exception) {

                }

                await Task.Delay(1000 / 90, cts.Token);
            }
        }


        public Dictionary<string, ParameterInfo[]> GetFeatures() => null;
       

        public Type GetConfigBody() => null;
    }
}
