using ForzaHorizon5Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using YawGLAPI;
namespace ForzaHorizon5Plugin
{
    [Export(typeof(Game))]
	[ExportMetadata("Name", "Forza Horizon 5")]
	[ExportMetadata("Version", "1.1")]
	public class ForzaHorizon5Plugin : Game {
		

		private bool stop = false;
		private Thread readthread;
		UdpClient receivingUdpClient;
		IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;

        public string PROCESS_NAME => "ForzaHorizon5";
		public int STEAM_ID => 1551360;
		public bool PATCH_AVAILABLE => true;
		public string AUTHOR => "YawVR";

		public string Description => Resources.description;

		public Stream Logo => GetStream("logo.png");
		public Stream SmallLogo => GetStream("recent.png");
		public Stream Background => GetStream("wide.png");



		public LedEffect DefaultLED() {
			return new LedEffect(

		   EFFECT_TYPE.KNIGHT_RIDER_2,
		   4,
		   new YawColor[] {
				new YawColor(66, 135, 245),
				 new YawColor(80,80,80),
				new YawColor(128, 3, 117),
				new YawColor(110, 201, 12),
				},
		   25f);

		}

		public List<Profile_Component> DefaultProfile() {

			return dispatcher.JsonToComponents(Resources.defProfile);

		}

		public void Exit() {
			receivingUdpClient.Close();
			receivingUdpClient = null;
			stop = true;
			//readthread.Abort();

		}
		public string[] GetInputData() {

			Type t = typeof(ForzaTelemetry);
			FieldInfo[] fields = t.GetFields();

			string[] inputs = new string[fields.Length];

			for (int i = 0; i < fields.Length; i++) {
				inputs[i] = fields[i].Name;
			}
			return inputs;
		}


		
		public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
		{
			this.dispatcher = dispatcher;
			this.controller = controller;
		}
		public void Init() {
			Addloopback();
			stop = false;
		
			var pConfig = dispatcher.GetConfigObject<Config>();
			receivingUdpClient = new UdpClient(pConfig.Port);
			receivingUdpClient.Client.ReceiveTimeout = 2000;
			readthread = new Thread(new ThreadStart(ReadFunction));
			readthread.Start();
		}

		private void ReadFunction() {

			Console.WriteLine("ForzaRD");
			ForzaTelemetry obj = new ForzaTelemetry();
			FieldInfo[] fields = typeof(ForzaTelemetry).GetFields();
			try
			{
				while (!stop)
				{
					try
					{
						// Blocks until a message returns on this socket from a remote host.
						byte[] rawData = receivingUdpClient.Receive(ref RemoteIpEndPoint);
						Console.Write(BitConverter.ToSingle(rawData, 0));

						IntPtr unmanagedPointer = Marshal.AllocHGlobal(rawData.Length);
						Marshal.Copy(rawData, 0, unmanagedPointer, rawData.Length);
						// Call unmanaged code
						Marshal.FreeHGlobal(unmanagedPointer);
						Marshal.PtrToStructure(unmanagedPointer, obj);

						obj.Yaw *= 57.295f;
						obj.Pitch *= 57.295f;
						obj.Roll *= 57.295f;

						obj.AngularVelocityX *= 57.295f;
						obj.AngularVelocityY *= 57.295f;
						obj.AngularVelocityZ *= 57.295f;
						obj.speed = 4 * (float)Math.Sqrt(Math.Pow(obj.VelocityX, 2) + Math.Pow(obj.VelocityY, 2) + Math.Pow(obj.VelocityZ, 2));
						if (obj.IsRaceOn == 1)
						{
							for (int i = 0; i < fields.Length; i++)
							{
								controller.SetInput(i, (float)Convert.ChangeType(fields[i].GetValue(obj), TypeCode.Single));
							}
						}

					}
					catch (SocketException) { }

				}
			} catch(ObjectDisposedException)
			{
				dispatcher.ExitGame();
			}
		}


		public void PatchGame() {

			Addloopback();


		}
		public void Addloopback() {
			var proc1 = new ProcessStartInfo();
			string anyCommand;
			proc1.UseShellExecute = true;

			proc1.WorkingDirectory = @"C:\Windows\System32";

			proc1.FileName = @"C:\Windows\System32\cmd.exe";
			proc1.Verb = "runas";
			proc1.Arguments = "/c " + "checknetisolation loopbackexempt -a -n=Microsoft.SunriseBaseGame_1.351.461.2_x64__8wekyb3d8bbwe";
			proc1.WindowStyle = ProcessWindowStyle.Normal;
			Process.Start(proc1);

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