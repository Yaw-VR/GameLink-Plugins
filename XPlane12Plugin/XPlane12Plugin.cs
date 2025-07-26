using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using XPlane11Plugin.Properties;
using YawGLAPI;

namespace XPlane11Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "X-Plane 12")]
    [ExportMetadata("Version", "2.0")]
    class XPlane11Plugin : Game {


        UdpClient udpClient;

        private Thread readThread;

        private volatile bool running = false;

        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 4123);
        IPEndPoint xplaneEndP = new IPEndPoint(IPAddress.Loopback,49000);
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        public string PROCESS_NAME => "X-Plane";
        public int STEAM_ID => 2014780;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => false;
        public string Description => Resources.description;

        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        public LedEffect DefaultLED() {
            return new LedEffect(

           EFFECT_TYPE.KNIGHT_RIDER,
           2,
           new YawColor[] {
                new YawColor(66, 135, 245),
                 new YawColor(80,80,80),
                new YawColor(128, 3, 117),
                new YawColor(110, 201, 12),
                },
           0.7f);
        }

        public List<Profile_Component> DefaultProfile() {

            return dispatcher.JsonToComponents(Resources.def_profile);
           /* return new List<Profile_Component>() {
                new Profile_Component(1,1, 1f,1f,0f,false,true,-1f,1f),
                new Profile_Component(2,2, 0.5f,0.5f,0f,false,true,-1f,1f),

                new Profile_Component(0,0, 1f,1f,0f,false,false,-1f,1f),
                new Profile_Component(6,1, 0.02f,0.02f,-500f,false,true,15f,1f),
                new Profile_Component(3,4, 0.04f,0.04f,-300f,false,false,-1f,1f),

                new Profile_Component(8,2, 0.02f,0.02f,-800f,false,true,12f,1f),
                new Profile_Component(3,3, 0.01f,0.01f,300f,false,false,10f,0.52f),
                new Profile_Component(15,4, 15f,15f,0f,true,false,-1,1f),
                new Profile_Component(14,3, 1f,1f,21f,false,false,-1,1f),
            };*/
        }

        public void Exit() {
            udpClient.Close();
            udpClient = null;
            running = false;
            //readThread.Abort();



        }

        public string[] GetInputData() {
            return new string[] {
                "Yaw","Pitch","Roll","RPM","Angular_Pitch","Angular_Roll","Gear1","Gear2","Gear3","ONGROUND","G_norml","G_axial","G_side","Engine_base","Speed","Aot_alpha","Aot_beta","Hpath","Vpath","Slip","Roll_force"
            };
        }


        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

        public void Init() {

            var pConfig = dispatcher.GetConfigObject<Config>();
            Console.WriteLine("XPLANE12 INIT");
            running = true;
            udpClient = new UdpClient(pConfig.Port);
            udpClient.Client.ReceiveTimeout = 5000;
            readThread = new Thread(new ThreadStart(ReadFunction));

            readThread.Start();

        }
        private void ReadFunction() {

            float pitch = 0, roll = 0, yaw = 0, engine_rpm = 0, ang_pitch = 0, ang_roll = 0, speeds = 0,aot_alpha = 0,aot_beta = 0,hpath = 0,vpath = 0,slip = 0;
            float engine_base = 0;
            float g_norml = 0, g_axial = 0, g_side = 0;
            float[] gears = new float[3];


            while (running) {
                try
                {

                    byte[] rawData = udpClient.Receive(ref remote);

                    for (int i = 5; i < rawData.Length; i += (4 + 32))
                    {

                        switch (rawData[i])
                        {

                            case 3:
                                speeds = BitConverter.ToSingle(rawData, i + 4);
                                break;
                            //PITCH ROLL HEADING
                            case 17:
                                pitch = BitConverter.ToSingle(rawData, i + 4);
                                roll = BitConverter.ToSingle(rawData, i + 8);
                                yaw = BitConverter.ToSingle(rawData, i + 12);
                                break;

                            //RPM
                            case 37:
                                engine_rpm = BitConverter.ToSingle(rawData, i + 4);
                                engine_base = BitConverter.ToSingle(rawData, i + 4);
                                break;

                            //PITCH ROLL angular velocities
                            case 16:
                                // pitch_g = BitConverter.ToSingle(rawData, i + 4);
                                ang_roll = BitConverter.ToSingle(rawData, i + 8) * 10;
                                ang_pitch = BitConverter.ToSingle(rawData, i + 12) * 20;
                                break;

                            //landing gear vertical forces
                            case 66:
                                gears[0] = BitConverter.ToSingle(rawData, i + 4);
                                gears[1] = BitConverter.ToSingle(rawData, i + 8);
                                gears[2] = BitConverter.ToSingle(rawData, i + 12);
                                break;

                            //G LOADS
                            case 4:
                                g_norml = BitConverter.ToSingle(rawData, i + 5 * 4);
                                g_axial = BitConverter.ToSingle(rawData, i + 6 * 4);
                                g_side = BitConverter.ToSingle(rawData, i + 7 * 4);
                                break;

                            //AOT
                            case 18:
                                aot_alpha = BitConverter.ToSingle(rawData, i + 1 * 4);
                                aot_beta = BitConverter.ToSingle(rawData, i + 2 * 4);
                                hpath = BitConverter.ToSingle(rawData, i + 3 * 4);
                                vpath = BitConverter.ToSingle(rawData, i + 4 * 4);
                                slip = BitConverter.ToSingle(rawData, i + 8 * 4);
                                break;

                        }
                    }

                    controller.SetInput(0, yaw);
                    controller.SetInput(1, pitch);
                    controller.SetInput(2, roll);
                    controller.SetInput(3, engine_rpm);
                    controller.SetInput(4, ang_pitch);
                    controller.SetInput(5, ang_roll);
                    controller.SetInput(6, gears[0]);
                    controller.SetInput(7, gears[1]);
                    controller.SetInput(8, gears[2]);
                    controller.SetInput(9, (gears[0] <= 0f) ? 0f : 1f);
                    controller.SetInput(10, g_norml);
                    controller.SetInput(11, g_axial);
                    controller.SetInput(12, g_side);
                    controller.SetInput(13, engine_base - 1000f);
                    controller.SetInput(14, speeds);

                    controller.SetInput(15, aot_alpha);
                    controller.SetInput(16, aot_beta);
                    controller.SetInput(17, hpath);
                    controller.SetInput(18, vpath);
                    controller.SetInput(19, slip);

                    controller.SetInput(20, (float)(Math.Atan(g_side / g_norml) * (180f/Math.PI)));



                } catch(Exception) { }


            }

        }
        public void PatchGame()
        {
            return;
        }

        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            var ret = new Dictionary<string, ParameterInfo[]>();

            string[] methodNames = new string[] {
                nameof(Pause),nameof(ResetVR),nameof(ToggleVR), nameof(ResetRunway), nameof(ToggleRegular),
                nameof(ThrottleDown),nameof(ThrottleUp),nameof(SendCommand) };

            foreach (string methodName in methodNames)
            {
                var method = this.GetType().GetMethod(methodName);
                ret.Add(method.Name, method.GetParameters());
            }
            return ret;

        }


        public void ResetRunway()
        {
            SendCommand("sim/operation/reset_to_runway");
        }

        public void ToggleVR()
        {
            SendCommand("sim/VR/toggle_vr");
        }
        public void ResetVR()
        {
            SendCommand("sim/VR/general/reset_view");
        }
        public void ThrottleUp()
        {
            SendCommand("sim/engines/throttle_up");
        }
        public void ThrottleDown()
        {
            SendCommand("sim/engines/throttle_down");
        }
        public void ToggleRegular()
        {
            SendCommand("sim/flight_controls/brakes_toggle_regular");
        }
        public void Pause()
        {
            SendCommand(@"sim/operation/pause_toggle");
        }
        public void SendCommand(string command)
        {
            byte[] bytes = new byte[6 + command.Length];
            Buffer.BlockCopy(ASCIIEncoding.ASCII.GetBytes("CMND\0"), 0, bytes, 0, 5);
            Buffer.BlockCopy(ASCIIEncoding.ASCII.GetBytes(command), 0, bytes, 5, command.Length);
            bytes[bytes.Length - 1] = 0x00;
            udpClient.Send(bytes, bytes.Length,xplaneEndP);
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
