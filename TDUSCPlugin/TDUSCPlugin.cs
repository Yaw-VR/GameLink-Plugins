using Microsoft.VisualBasic;

using SharedLib;

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;

using TDUSCPlugin;

using YawGLAPI;


namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Test Drive Unlimited Solar Crown")]
    [ExportMetadata("Version", "1.0")]
    public class TDUSCPlugin : Game, IDisposable
    {
        
        //private Thread readThread;
        private IPEndPoint remoteIP;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        
        private bool isStarted = false;
        
        private bool _isRunning = false;
        private bool isRunning
        {
            get
            {
                return _isRunning;
            }
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    if (value)
                    {
                        stopwatch.Stop();
                        Log("r");
                    }
                    else
                    {
                        stopwatch.Restart();
                        Log("X");
                        
                    }
                }
                
                    
            }
        }


        
        private int gameport = 20777;
        bool testing = false;
        private UdpClient socket;
        private const float _sample_rate = 1 / 60f;
        
        
        private TDUSCPacket _lastpacket;        
        private Vector3 _previous_local_velocity;

        Stopwatch stopwatch = new Stopwatch();

        public string PROCESS_NAME => "TDUSC";
        public int STEAM_ID => 1249970;
        
        public bool PATCH_AVAILABLE => true;
        public string AUTHOR => "Trevor Jones (Drowhunter)";

        public Stream Logo => ResourceHelper.GetStream("logo.png");

        public Stream SmallLogo => ResourceHelper.GetStream("recent.png");

        public Stream Background => ResourceHelper.GetStream("wide.png");

        public string Description => ResourceHelper.GetString("description.html");

        private string defProfilejson => ResourceHelper.GetString("Default.yawglprofile");

        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(defProfilejson);

        public LedEffect DefaultLED() => dispatcher.JsonToLED(defProfilejson);

        

        public TDUSCPlugin()
        {
            
        }


        public TDUSCPlugin(IProfileManager controller,IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }

        public void Exit()
        {
            isStarted = false;
            isRunning = false;
            Dispose();           
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }

        void Log(string message)
        {
            Console.Write(message);
        }


        public void Init()
        {
            var pConfig = dispatcher.GetConfigObject<Config>();
            gameport = pConfig.Port;

            bool error = false;
            do
            {
                try
                {
                    socket = new UdpClient(gameport);
                    socket.Client.ReceiveTimeout = 60000;
                    var thread = new Thread(ReadFunction);
                    thread.Name = "TDUSC Read Thread";
                    thread.Start();

                    isStarted = true;
                    error = false;
                }
                catch (SocketException ex)
                {
                    error = true;
                    dispatcher.ShowNotification(NotificationType.ERROR, ex.Message);
                    
                }
                catch (Exception ex)
                {
                    dispatcher.ShowNotification(NotificationType.ERROR, ex.Message);
                    isStarted = false;
                    return;
                }
            } while (error);
        }

        

        public void ReadFunction()
        {
            while (isStarted)
            {
                try
                {
                    if(_lastpacket == null)
                    {
                        Log("s");
                    }
                    

                    var buffer = socket.Receive(ref remoteIP);

                    if (buffer.Length >= 280)
                    {
                        if (isRunning == false)
                        {
                            Log("R");
                            isRunning = true;
                            socket.Client.ReceiveTimeout = 900;
                        }

                        _lastpacket = new TDUSCPacket(buffer);
                        SetInput(isRunning);    
                    }
                    else
                    {
                        Log("!");
                        isRunning = false;
                        
                    }
                }
                catch (SocketException ex)
                {
                    if (stopwatch.Elapsed < TimeSpan.FromMinutes(10))
                    {
                        if (isRunning)
                        {
                            isRunning = false;
                        }

                        if (_lastpacket != null)
                        {

                            SetInput(isRunning);
                        }
                    }
                    else {
                        Log(Environment.NewLine + "Q");
                        isStarted = false;
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.Message+Environment.NewLine);
                    isStarted = false;
                    isRunning= false;
                }
                
            }
        }

        

        public void SetInput(bool isAlive)
        {
            var packet = _lastpacket;

            var v_world = new Vector3(packet.VelocityX, packet.VelocityY, packet.VelocityZ);
            var rollV = new Vector3(packet.RollX, packet.RollY, packet.RollZ);
            var pitchV = new Vector3(packet.PitchX, packet.PitchY, packet.PitchZ);

            Vector3 yawV = rollV.Cross(pitchV);
            
            var Q = System.Numerics.Quaternion.CreateFromRotationMatrix(
                new Matrix4x4(
                    rollV.X, pitchV.X, yawV.X, 0.0f, 
                    rollV.Y, pitchV.Y, yawV.Y, 0.0f, 
                    rollV.Z, pitchV.Z, yawV.Z, 0.0f, 
                    0.0f, 0.0f, 0.0f, 1f)
                );

            (float pitch, float yaw, float roll) = Q.ToEuler();

            Vector3 local_velocity = Maths.WorldtoLocal(Q, v_world);

            
            float surge = 0.0f;
            float heave = 0.0f;

            Vector3 previousLocalVelocity = _previous_local_velocity;
            if (_previous_local_velocity !=  default)
            {
                var delta_velocity = local_velocity - _previous_local_velocity;

                surge = delta_velocity.Z / _sample_rate / 9.81f;
                heave = delta_velocity.Y / _sample_rate / 9.81f;
            }

            if (!testing)
            {
                this.controller.SetInput(0, packet.Speed);
                this.controller.SetInput(1, packet.RPM);
                this.controller.SetInput(2, packet.Steer);
                this.controller.SetInput(3, packet.Gforce_lon);
                this.controller.SetInput(4, packet.Gforce_lat);
                this.controller.SetInput(5, -pitch);
                this.controller.SetInput(6, yaw);
                this.controller.SetInput(7, -roll);
                this.controller.SetInput(8, packet.Susp_pos_bl);
                this.controller.SetInput(9, packet.Susp_pos_br);
                this.controller.SetInput(10, packet.Susp_pos_fl);
                this.controller.SetInput(11, packet.Susp_pos_fr);
                this.controller.SetInput(12, packet.Susp_vel_bl);
                this.controller.SetInput(13, packet.Susp_vel_br);
                this.controller.SetInput(14, packet.Susp_vel_fl);
                this.controller.SetInput(15, packet.Susp_vel_fr);
                this.controller.SetInput(16, local_velocity.X);
                this.controller.SetInput(17, local_velocity.Y);
                this.controller.SetInput(18, local_velocity.Z);
                this.controller.SetInput(19, isAlive ? 1f : 0.0f);
                this.controller.SetInput(20, surge);
                this.controller.SetInput(21, heave);
            }

            Log(isRunning ? "." : "x");
            _previous_local_velocity = local_velocity;
        }

        public string[] GetInputData()
        {
            return new string[22]
            {
                "Speed",
                "RPM",
                "Steer",
                "Force_long",
                "Force_lat",
                "Pitch",
                "Roll",
                "Yaw",
                "suspen_pos_bl",
                "suspen_pos_br",
                "suspen_pos_fl",
                "suspen_pos_fr",
                "suspen_vel_bl",
                "suspen_vel_br",
                "suspen_vel_fl",
                "suspen_vel_fr",
                "VelocityX",
                "VelocityY",
                "VelocityZ",
                "IsAlive",
                "Surge",
                "Sway"
            };
        }

        public Dictionary<string, ParameterInfo[]> GetFeatures() => null;
        
        public void PatchGame()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\My Games\Test Drive Unlimited Solar Crown\UserSettings.cfg";
            if (File.Exists(path))
            {
                var pConfig = dispatcher.GetConfigObject<Config>();
                gameport = pConfig.Port;
               

                string input = File.ReadAllText(path);
                string contents = Regex.Replace(input, @"(?<=Rc2\.Telemetry\.EnableTelemetry\s*?=\s*?)false", "true", RegexOptions.Singleline);
                contents = Regex.Replace(contents, @"(?<=Rc2.Telemetry.TelemetryPort\s*?=\s*?)\d+", gameport+"", RegexOptions.Singleline); 
                if (!(contents != input))
                    return;
                File.WriteAllText(path, contents);
                dispatcher.ShowNotification(NotificationType.INFO, path + " patched!");
            }
            else
                dispatcher.DialogShow("Could not find UserSetting.cfg, Make sure you run the game at least once first", DIALOG_TYPE.INFO);
        }

        public void Dispose()
        {
            try
            {
                socket.Close();
            }
            finally
            {
                socket = null;                
            }
        }

        public Type GetConfigBody()
        {
            return typeof(Config);
        }
    }
}
