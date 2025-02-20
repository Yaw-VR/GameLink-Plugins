using OverloadPlugin;

using SharedLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Overload")]
    [ExportMetadata("Version", "1.3")]
    class OverloadPlugin : Game 
    {
        #region Standard Plugin Properties
        public string PROCESS_NAME => "olmod";
        public int STEAM_ID => 0;
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "PhunkaeG, Trevor Jones (Drowhunter)";
        public string Description => ResourceHelper.GetString("description.html");
        public Stream Logo => ResourceHelper.GetStream("logo.png");
        public Stream SmallLogo => ResourceHelper.GetStream("recent.png");
        public Stream Background => ResourceHelper.GetStream("wide.png");

        #endregion

        

        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private bool running = false;
        private UdpClient udpClient;
        private IPEndPoint endPoint;
        private Thread readThread;

        

        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(ResourceHelper.GetString("Default.yawglprofile"));

        public LedEffect DefaultLED() => new(EFFECT_TYPE.KNIGHT_RIDER_2, 7,
            [
                new YawColor( 66,  135,  245),
                new YawColor( 80,  80,  80),
                new YawColor( 128,  3,  117),
                new YawColor( 110,  201,  12)
            ], 25f);

        public void Exit() {
            udpClient?.Close();
            udpClient = null;
            running = false;
        }

        public string[] GetInputData() {
            return [
                "pitch","yaw", "roll",
                "sway", "heave", "surge",
                "pitch_speed","yaw_speed", "roll_speed",
                "g_sway", "g_heave", "g_surge",
                "boosting", "primary_fire", "secondary_fire", "picked_up_item", "damage_taken"
            ];
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }


        public void Init()
        {
            if (udpClient != null)
                udpClient.Close();

            if (readThread != null && readThread.IsAlive)
                Exit();

            endPoint = new IPEndPoint(IPAddress.Any, 4123); // Use the correct port for Overload
            udpClient = new UdpClient(endPoint);

            running = true;
            readThread = new Thread(ReadTelemetry);
            readThread.Start();
        }


        private void ReadTelemetry()
        {
            try
            {
                while (running)
                {
                    if (udpClient.Available > 0)
                    {
                        byte[] data = udpClient.Receive(ref endPoint);
                        string telemetryData = Encoding.ASCII.GetString(data);
                        ProcessTelemetry(telemetryData);
                    }
                    // Thread.Sleep(20); // Reduce CPU usage - NB: Overloads sends UDP packets much too fast !!!
                }
            }
            catch (Exception ex)
            {
                // Handle or log exceptions
                Console.WriteLine("Error reading telemetry data: " + ex.Message);
            }
        }
        


        private void ProcessTelemetry(string telemetry)
        {
            PlayerData pd = null;
            try
            {
                pd = PlayerData.Parse(telemetry);
                if (pd != null)
                {

                    controller.SetInput(0, pd.Rotation.X);
                    controller.SetInput(1, pd.Rotation.Y);
                    controller.SetInput(2, pd.Rotation.Z);

                    controller.SetInput(3, pd.LocalVelocity.X);
                    controller.SetInput(4, pd.LocalVelocity.Y);
                    controller.SetInput(5, pd.LocalVelocity.Z);

                    controller.SetInput(6, pd.LocalAngularVelocity.X);
                    controller.SetInput(7, pd.LocalAngularVelocity.Y);
                    controller.SetInput(8, pd.LocalAngularVelocity.Z);

                    controller.SetInput(9, pd.LocalGForce.X);
                    controller.SetInput(10, pd.LocalGForce.Y);
                    controller.SetInput(11, pd.LocalGForce.Z);

                    controller.SetInput(12, pd.EventBoosting);
                    controller.SetInput(13, pd.EventPrimaryFire);
                    controller.SetInput(14, pd.EventSecondaryFire);
                    controller.SetInput(15, pd.EventItemPickup);
                    controller.SetInput(16, pd.EventDamageTaken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing telemetry data: {ex.Message}");
            }
        }

        public void PatchGame()
        {

        }


        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return null;
        }

        public Type GetConfigBody()
        {
            return null;
        }
    }
}
