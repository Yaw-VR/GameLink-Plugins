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
    [ExportMetadata("Version", "1.1")]

    public class DistancePlugin : Game
    {



        private UdpTelemetry<DistanceTelemetryData> telem;
        private Thread readThread;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        private volatile bool running = false;
        public int STEAM_ID => 233610;
        public string PROCESS_NAME => "Distance";
        public string AUTHOR => "Trevor Jones";

        public bool PATCH_AVAILABLE => false;

        public string Description => ResourceHelper.GetString("description.html");
        public Stream Logo => ResourceHelper.GetStream("logo.png");
        public Stream SmallLogo => ResourceHelper.GetStream("recent.png");
        public Stream Background => ResourceHelper.GetStream("wide.png");
        private string defProfile => ResourceHelper.GetString("Default.yawglprofile");

        private IDeviceParameters deviceParameters;

        public Dictionary<string, ParameterInfo[]> GetFeatures() => null;

        public void Exit()
        {
            running = false;
            telem?.Dispose();
        }
        public void PatchGame()
        {
            return;
        }


        public string[] GetInputData() => InputHelper.GetInputs<DistanceTelemetryData>(default).Select(_ => _.key).ToArray();

        public LedEffect DefaultLED() => new LedEffect(EFFECT_TYPE.KNIGHT_RIDER, 0, new[] { YawColor.WHITE }, 0); 
        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(defProfile);
            
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

        public void Init()
        {
            deviceParameters = dispatcher.GetDeviceParameters();
            running = true;
            readThread = new Thread(new ThreadStart(ReadThread));
            readThread.Start();
        }
        private void ReadThread()
        {
            try
            {
                telem = new UdpTelemetry<DistanceTelemetryData>(new UdpTelemetryConfig
                {
                    ReceiveAddress = new IPEndPoint(IPAddress.Any, 12345)
                });
            
            }
            catch(Exception x)
            {
                dispatcher.ShowNotification(NotificationType.ERROR, x.Message);
                Exit();
            }

            while (running)
            {
                try
                {
                    var data = telem.Receive();

                    if (data.IsCarIsActive)
                    {
                        if (!isRestting)
                        {
                            foreach (var (i, (key, value)) in InputHelper.GetInputs(data).WithIndex())
                            {
                                if (key == "Pitch" || key == "Roll")
                                {
                                    var nv = value;

                                    //var v = 0f;
                                    //if (MathF.Abs(value) <= 90)
                                    //{
                                    //    v = value;
                                    //}
                                    //else
                                    //{
                                    //    v = MathF.CopySign(180 - MathF.Abs(value), value);
                                    //}
                                    

                                    //if (key == "Pitch")
                                    //{
                                    //    nv = ScalePitchRoll(value, deviceParameters.PitchLimitF, deviceParameters.PitchLimitB);
                                    //}
                                    //else if (key == "Roll")
                                    //{
                                    //   nv = ScalePitchRoll(value, deviceParameters.RollLimit, deviceParameters.RollLimit);

                                    //}

                                    controller.SetInput(i, nv);
                                    continue;
                                }

                                controller.SetInput(i, value);
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
                catch(SocketException sex) { }
            }
            
        }

        bool isRestting = false;

        private float ScalePitchRoll(float value, float fwMax,  float bkMax)
        {
            return MathsF.EnsureMapRange(value, 0, MathF.CopySign(90, value), 0, value < 0 ? fwMax: bkMax);            
        }


        

        
    }

    static class Extensions
    {
        

        public static IEnumerable<(int index, T value)> WithIndex<T>(this IEnumerable<T> source)
        {
            int index = 0;
            foreach (var item in source)
            {
                yield return (index, item);
                index++;
            }
        }
    }

}