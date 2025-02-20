using Codemasters.F1_2020;
using F12022Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using YawGLAPI;

namespace F12022Plugin
{

    [Export(typeof(Game))]
    [ExportMetadata("Name", "F1 2022")]
    [ExportMetadata("Version", "1.1")]
    public class F12022Plugin : Game
    {

        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        private volatile bool running = false;
        private Thread readthread;
        UdpClient receivingUdp;
        IPEndPoint RemoteIpEnd = new IPEndPoint(IPAddress.Any, 20777);

        FieldInfo[] fields;
        MotionPacket mp = new MotionPacket();
        TelemetryPacket tp = new TelemetryPacket();

        public int STEAM_ID => 1692250;

        public string PROCESS_NAME => "F1_22";

        public bool PATCH_AVAILABLE => false;

        public string AUTHOR => "YawVR";

        public Stream Logo => GetStream("logo.png");

        public Stream SmallLogo => GetStream("recent.png");

        public Stream Background => GetStream("wide.png");

        public string Description => Resources.description;

        public LedEffect DefaultLED()
        {
            return dispatcher.JsonToLED(Resources.profile);
        }

        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(Resources.profile);
        }

        public void Exit()
        {
            running = false;
            receivingUdp.Close();
            receivingUdp = null;
        }

        public string[] GetInputData()
        {
            string[] inputs = new string[] {
                "Yaw","Pitch","Roll","VelocityX","VelocityY","VelocityZ","G_Lateral","G_Longitudinal","G_Vertical",
                "Wheelslip_RL","Wheelslip_RR","Wheelslip_FL","Wheelslip_FR",
                "SuspensionAcc_RL","SuspensionAcc_RR","SuspensionAcc_FL","SuspensionAcc_FR",
                "SuspensionPos_RL","SuspensionPos_RR","SuspensionPos_FL","SuspensionPos_FR",
                "RevLightPercentage","Throttle","Brake","DrsActive"
            };
            return inputs;
        }

        public void Init()
        {
            if (fields == null) fields = typeof(MotionPacket).GetFields();

            var pConfig = dispatcher.GetConfigObject<Config>();
            receivingUdp = new UdpClient(pConfig.Port);
            receivingUdp.Client.ReceiveTimeout = 5000;

            running = true;
            readthread = new Thread(new ThreadStart(ReadFunction));
            readthread.Start();
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


        private void ReadFunction()
        {
            try
            {
                while (running)
                {
                    try
                    {
                        byte[] data = receivingUdp.Receive(ref RemoteIpEnd);
                        PacketType pt = CodemastersToolkit.GetPacketType(data);
                        switch (pt)
                        {
                            case PacketType.Motion:
                                mp.LoadBytes(data);

                                MotionPacket.CarMotionData cardata = mp.FieldMotionData[mp.PlayerCarIndex];

                                controller.SetInput(0, cardata.Yaw * 57.2957795f);
                                controller.SetInput(1, cardata.Pitch * 57.2957795f);
                                controller.SetInput(2, cardata.Roll * 57.2957795f);

                                controller.SetInput(3, cardata.VelocityX);
                                controller.SetInput(4, cardata.VelocityY);
                                controller.SetInput(5, cardata.VelocityZ);

                                controller.SetInput(6, cardata.gForceLateral);
                                controller.SetInput(7, cardata.gForceLongitudinal);
                                controller.SetInput(8, cardata.gForceVertical);

                                controller.SetInput(9, mp.WheelSlip.RearLeft);
                                controller.SetInput(10, mp.WheelSlip.RearRight);
                                controller.SetInput(11, mp.WheelSlip.FrontLeft);
                                controller.SetInput(12, mp.WheelSlip.FrontRight);

                                controller.SetInput(13, mp.SuspensionAcceleration.RearLeft);
                                controller.SetInput(14, mp.SuspensionAcceleration.RearRight);
                                controller.SetInput(15, mp.SuspensionAcceleration.FrontLeft);
                                controller.SetInput(16, mp.SuspensionAcceleration.FrontRight);


                                controller.SetInput(17, mp.SuspensionPosition.RearLeft);
                                controller.SetInput(18, mp.SuspensionPosition.RearRight);
                                controller.SetInput(19, mp.SuspensionPosition.FrontLeft);
                                controller.SetInput(20, mp.SuspensionPosition.FrontRight);

                                break;
                            case PacketType.CarTelemetry:
                                tp.LoadBytes(data);
                                TelemetryPacket.CarTelemetryData tdata = tp.FieldTelemetryData[tp.PlayerCarIndex];

                                controller.SetInput(21, (float)tdata.RevLightsPercentage);
                                controller.SetInput(22, tdata.Throttle);
                                controller.SetInput(23, tdata.Brake);
                                controller.SetInput(24, tdata.DrsActive ? 1 : 0);

                                break;

                        }
                    } catch(SocketException) { }
                    // TelemetryPacket tp = new TelemetryPacket();
                    //tp.LoadBytes(data);
                }
            }
            catch (SocketException)
            {
              //  dispatcher.ExitGame();
            }
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
            return typeof(Config);
        }
    }
}
