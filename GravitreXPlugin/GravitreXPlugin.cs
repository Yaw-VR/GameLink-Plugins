using GravitreXPlugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using YawGLAPI;

namespace GravitreXPlugin
{
    [Export(typeof(Game))]
	[ExportMetadata("Name", "GravitreX Arcade")]
	[ExportMetadata("Version", "1.0")]
	public class GravitreXPlugin : Game
	{
		public int STEAM_ID => 1483610;

		public string PROCESS_NAME => "GravitreX Arcade";

		public bool PATCH_AVAILABLE => true;

		public string AUTHOR => "Martin Hjerne";

		public Stream Logo => GetStream("logo.png");

		public Stream SmallLogo => GetStream("recent.png");

		public Stream Background => GetStream("wide.png");

		public string Description => "If patch button doesn't work, change 'EnableTelemetry = false' to true in the file Telemtry.cfg in the game's folder.";

		private Thread readThread;
		private bool running = false;
		private IProfileManager controller;
		private IMainFormDispatcher dispatcher;
		private UdpClient client;

		public LedEffect DefaultLED()
		{
            return new LedEffect(

               EFFECT_TYPE.FLOW_LEFTRIGHT,
               0,
               new YawColor[] {
                    new YawColor(52, 235, 192),
                    new YawColor(20,20,20),
                    new YawColor(235, 52, 137),
                    new YawColor(235, 165, 52),
                    },
                1f);
        }

		public List<Profile_Component> DefaultProfile()
		{
            var profile = new List<Profile_Component>
            {
                new Profile_Component(5, 2, 0.5f, 0.5f, 0, false, false, -1, 0.05f, true, null, new ProfileSpikeflatter(true, 70, 0.5f) { Enabled = true }),
                new Profile_Component(6, 1, 0.05f, 0.05f, 0, false, false, -1, 0.05f, true, null, new ProfileSpikeflatter(true, 70, 0.5f) { Enabled = true }),
                new Profile_Component(2, 2, 0.03f, 0.03f, 0, false, false, -1, 0.05f, true, null, new ProfileSpikeflatter(true, 70, 0.5f) { Enabled = true }),
                new Profile_Component(1, 2, 0.04f, 0.04f, 0, false, false, -1, 0.05f, true, null, new ProfileSpikeflatter(true, 70, 0.5f) { Enabled = true })
            };
            return profile;
		}

		public void Exit()
		{
			client.DropMulticastGroup(IPAddress.Parse("FF01::1"));
			client.Close();
			running = false;
		}

		public Dictionary<string, ParameterInfo[]> GetFeatures()
		{			
			return null;
		}

		public string[] GetInputData()
		{
			return new string[] { "Roll", "RollSpeed", "RollAcceleration", "XSpeed", "YSpeed", "XAcceleration", "YAcceleration", "XAcceleration_noGravity", "YAcceleration_noGravity" };
		}

		public void Init()
		{
			running = true;
			readThread = new Thread(() =>
				{

					int port = dispatcher.GetConfigObject<Config>().Port;
					var ep = new IPEndPoint(IPAddress.Parse("FF01::1"), port);
					IPv6MulticastOption ipv6MulticastOption = new IPv6MulticastOption(ep.Address);
					IPAddress group = ipv6MulticastOption.Group;
					long interfaceIndex = ipv6MulticastOption.InterfaceIndex;
					client = new UdpClient(port, AddressFamily.InterNetworkV6);
					client.JoinMulticastGroup((int)interfaceIndex, group);

					while (running)
					{
						//READ
						try
						{
							var bytes = client.Receive(ref ep);
							var packet = GravitreX.TelemetryPacket.FromBytes(bytes);

							controller.SetInput(0, packet.roll);
							controller.SetInput(1, packet.rollSpeed);
							controller.SetInput(2, packet.rollAcceleration);
							controller.SetInput(3, packet.XSpeed);
							controller.SetInput(4, packet.YSpeed);
							controller.SetInput(5, packet.XAcceleration);
							controller.SetInput(6, packet.YAcceleration);
							controller.SetInput(7, packet.XAcceleration_noGravity);
							controller.SetInput(8, packet.YAcceleration_noGravity);
							//Thread.Sleep(20);
						}
						catch
						{
							running = false;
							if (client.Client != null && client.Client.Connected)
								client.DropMulticastGroup(IPAddress.Parse("FF01::1"));
							client.Close();
							break;
						}
					}
				});
			readThread.Start();
		}

		public void PatchGame()
		{
			string path = Path.Combine(dispatcher.GetInstallPath("GravitreX Arcade"), "Telemetry.cfg");
			if (!File.Exists(path))
			{
				dispatcher.DialogShow("Couldn't find Telemetry.cfg to patch. Is the game up to date? Path: " + path, DIALOG_TYPE.INFO);
				return;
			}
			//Patch the file!
			string contents = File.ReadAllText(path);
			if (!contents.Contains("TelemetryEnabled = false"))
			{
				if (contents.Contains("TelemetryEnabled = true"))
					dispatcher.DialogShow("Telemetry already enabled, you're good to go!", DIALOG_TYPE.INFO);
				else
					dispatcher.DialogShow("Telemetry.cfg is corrupt. Verify game files integrity.", DIALOG_TYPE.INFO);
				return;
			}
			else
			{
				contents = contents.Replace("TelemetryEnabled = false", "TelemetryEnabled = true");
				File.WriteAllText(path, contents);
				dispatcher.DialogShow("Telemetry enabled, you're good to go!", DIALOG_TYPE.INFO);
			}
		}

		public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
		{
			this.controller = controller;
			this.dispatcher = dispatcher;
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
