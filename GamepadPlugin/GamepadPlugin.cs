using GamepadPlugin.Properties;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Gamepad")]
    [ExportMetadata("Version", "1.0")]
    class GamepadPlugin : Game {
       
        Thread readThread;
        State lastState;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => false;
        public int STEAM_ID => 0;
        public string PROCESS_NAME => string.Empty;

        public string Description => Resources.description;
        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");
        private bool running = false;

        // Get 1st controller available
        Controller gamepad = null;
        public void PatchGame() {
            return;
        }

        public List<Profile_Component> DefaultProfile() {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }
        public LedEffect DefaultLED() {
            return dispatcher.JsonToLED(Resources.defProfile);
        }

        public void Exit() {

            running = false;
        }

        public string[] GetInputData() {
            return new string[] {
                "LeftThumbX","LeftThumbY","RightThumbX","RightThumbY","LeftTrigger","RightTrigger","LeftShoulder","RightShoulder","DPadUp","DPadRight","DPadDown","DPadLeft","Y","B","A","X","LeftStick","RightStick"
            };
        }
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
        public void Init() {
            running = true;
            readThread = new Thread(new ThreadStart(ReadFunction));
            readThread.Start();
        }

        private void ReadFunction() {
            Debug.WriteLine("Start XGamepadApp");
            Thread.Sleep(200);
            // Initialize XInput
            var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };

           
            foreach (var selectControler in controllers) {
                if (selectControler.IsConnected) {
                    gamepad = selectControler;
                    break;
                }
            }

            if(!(gamepad is null) && gamepad.IsConnected) {

                Console.WriteLine("Found a XInput controller available");
          
                // Poll events from joystick
                var previousState = gamepad.GetState();
                Vibration v = new Vibration();
                
                v.LeftMotorSpeed = 65535;
                v.RightMotorSpeed = 65535;
                Task.Run(async () =>
                {
                    await Task.Delay(600);
                    gamepad.SetVibration(new Vibration());
                }
                    );
                gamepad.SetVibration(v);
                while (gamepad.IsConnected && running) {
                   
                    lastState = gamepad.GetState();
                    //   if (previousState.PacketNumber != state.PacketNumber)

                    controller.SetInput(0, DeadZone(lastState.Gamepad.LeftThumbX / 32768f,0.1f));
                    controller.SetInput(1, DeadZone(lastState.Gamepad.LeftThumbY / 32768f, 0.1f));
                    controller.SetInput(2, DeadZone(lastState.Gamepad.RightThumbX / 32768f, 0.1f));
                    controller.SetInput(3, DeadZone(lastState.Gamepad.RightThumbY / 32768f, 0.1f));
                 
                    controller.SetInput(4, lastState.Gamepad.LeftTrigger / 255f);
                    controller.SetInput(5, lastState.Gamepad.RightTrigger / 255f);

                    controller.SetInput(6, ButtonPressed(GamepadButtonFlags.LeftShoulder));
                    controller.SetInput(7, ButtonPressed(GamepadButtonFlags.RightShoulder));

                    controller.SetInput(8, ButtonPressed(GamepadButtonFlags.DPadUp));
                    controller.SetInput(9, ButtonPressed(GamepadButtonFlags.DPadRight));
                    controller.SetInput(10, ButtonPressed(GamepadButtonFlags.DPadDown));
                    controller.SetInput(11, ButtonPressed(GamepadButtonFlags.DPadLeft));

                    controller.SetInput(12, ButtonPressed(GamepadButtonFlags.Y));
                    controller.SetInput(13, ButtonPressed(GamepadButtonFlags.B));
                    controller.SetInput(14, ButtonPressed(GamepadButtonFlags.A));
                    controller.SetInput(15, ButtonPressed(GamepadButtonFlags.X));

                    controller.SetInput(16, ButtonPressed(GamepadButtonFlags.LeftThumb));
                    controller.SetInput(17, ButtonPressed(GamepadButtonFlags.RightThumb));

                    Thread.Sleep(10);
                    previousState = lastState;
                }
            }
            if(gamepad?.IsConnected == false || gamepad is null) NoController();
        }

        private void NoController()
        {
            dispatcher.DialogShow("No XBOX controller!",DIALOG_TYPE.INFO);
            dispatcher.ExitGame();
        }
        private float DeadZone(float v,float min) {
            if (-min < v && v  < min) return 0;
            return v;
        }

        private float ButtonPressed(GamepadButtonFlags flag) {
            return lastState.Gamepad.Buttons.HasFlag(flag) ? 1 : 0;
        }

        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
           var dict = new Dictionary<string, ParameterInfo[]>();

            dict.Add(nameof(Vibrate), this.GetType().GetMethod(nameof(Vibrate)).GetParameters());

            return dict;
        }

        public void Vibrate(ushort left, ushort right,int length = 1000)
        {
            Vibration v = new Vibration();

            v.LeftMotorSpeed = left;
            v.RightMotorSpeed = right;
            Task.Run(async () =>
            {
                await Task.Delay(length);
                gamepad?.SetVibration(new Vibration());
            }
                );
            gamepad?.SetVibration(v);

            
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
            return null;
        }
    }
}
