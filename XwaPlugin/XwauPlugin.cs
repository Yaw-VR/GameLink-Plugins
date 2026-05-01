using Newtonsoft.Json;

using SharedLib;
using SharedLib.TelemetryHelper;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

using YawGLAPI;
// 
namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "X-Wing Alliance")]
    [ExportMetadata("Version", "1.0")]
    public class XwauPlugin : Game
    {
        

        #region Standard Properties
        public int STEAM_ID => 0;
        public string PROCESS_NAME => "xwingalliance";
        public string AUTHOR => "Trevor Jones (Drowhunter)";

        public bool PATCH_AVAILABLE => false;

        
        public string Description => ResourceHelper.Description;
        public Stream Logo => ResourceHelper.Logo;
        public Stream SmallLogo => ResourceHelper.SmallLogo;
        public Stream Background => ResourceHelper.Background;
        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(ResourceHelper.DefaultProfile);

        
        public LedEffect DefaultLED() => new LedEffect(EFFECT_TYPE.KNIGHT_RIDER, 0, [YawColor.WHITE], 0);

        public Dictionary<string, ParameterInfo[]> GetFeatures() => null;

        public Type GetConfigBody() => typeof(Config);
        
        
        private Config settings;
        #endregion

        public string[] GetInputData() => InputHelper.GetValues<Telemetry>().Keys();


        private volatile bool running = false;
        private UdpTelemetry<XwaPlayer> telem;
        private Thread readThread;
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        CancellationTokenSource cts = new();
        

        public void Exit()
        {
            running = false;
            cts.Cancel();
            telem?.Dispose();
        }

       
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }

        public void Init()
        {
            this.settings = dispatcher.GetConfigObject<Config>();
           
            running = true;
            readThread = new Thread(new ThreadStart(ReadThread));
            readThread.Start();

        }

        private void ReadThread()
        {
            var options = new JsonSerializerSettings()
            {
                // Keep defaults; add our converter
            };
            // Use Newtonsoft converter for "0"/"1" boolean handling
            options.Converters.Add(new ZeroOneBooleanNewtonsoftConverter());
            
            telem = new UdpTelemetry<XwaPlayer>(new UdpTelemetryConfig
            {
                ReceiveAddress = new IPEndPoint(IPAddress.Parse(settings.IP), settings.Port)
            }, new NewtonsoftJsonConverter<XwaPlayer>(options));

            double yaw, pitch, roll = 0.0f;
            while (running)
            {
                try
                {
                    var x = telem.Receive();
                    var telemetry = (Telemetry)x;

                    var values = InputHelper.GetValues(telemetry);

                    var list = new List<(string key, float value)>(values);
                    for (var i = 0; i < list.Count; i++)
                    {
                        
                        var (k, value) = list[i];
                        controller.SetInput(i, value);
                    }
                }
                catch (SocketException) { }
            }
            
        }


        public async void PatchGame()
        {
#if DEBUG
            Debugger.Launch();
#endif                        
        }

        // Custom converter for "0"/"1" to bool
        private sealed class ZeroOneBooleanConverter : System.Text.Json.Serialization.JsonConverter<bool>
        {
            public override bool Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case System.Text.Json.JsonTokenType.True:
                        return true;
                    case System.Text.Json.JsonTokenType.False:
                        return false;
                    case System.Text.Json.JsonTokenType.String:
                        var s = reader.GetString();
                        if (string.Equals(s, "1", StringComparison.Ordinal) || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
                            return true;
                        if (string.Equals(s, "0", StringComparison.Ordinal) || string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                            return false;
                        // Fallback: try numeric parse
                        if (int.TryParse(s, out var n))
                            return n != 0;
                        throw new System.Text.Json.JsonException($"Invalid boolean string value: '{s}'.");
                    case System.Text.Json.JsonTokenType.Number:
                        if (reader.TryGetInt32(out var num))
                            return num != 0;
                        throw new System.Text.Json.JsonException("Invalid numeric boolean value.");
                    default:
                        throw new System.Text.Json.JsonException($"Unexpected token parsing boolean: {reader.TokenType}.");
                }
            }

            public override void Write(System.Text.Json.Utf8JsonWriter writer, bool value, System.Text.Json.JsonSerializerOptions options)
            {
                // Write as "1"/"0" strings to match input style
                writer.WriteStringValue(value ? "1" : "0");
            }
        }

        private sealed class ZeroOneBooleanNewtonsoftConverter : JsonConverter<bool>
        {
            public override bool CanRead => true;
            public override bool CanWrite => true;

            public override bool ReadJson(JsonReader reader, Type objectType, bool existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Boolean:
                        return (bool)reader.Value!;
                    case JsonToken.Integer:
                        try
                        {
                            var num = Convert.ToInt32(reader.Value, System.Globalization.CultureInfo.InvariantCulture);
                            return num != 0;
                        }
                        catch
                        {
                            throw new JsonSerializationException("Invalid numeric boolean value.");
                        }
                    case JsonToken.String:
                        var s = (reader.Value as string) ?? string.Empty;
                        if (string.Equals(s, "1", StringComparison.Ordinal) || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
                            return true;
                        if (string.Equals(s, "0", StringComparison.Ordinal) || string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                            return false;
                        if (int.TryParse(s, out var n))
                            return n != 0;
                        throw new JsonSerializationException($"Invalid boolean string value: '{s}'.");
                    case JsonToken.Null:
                        // Default to false for nulls; adjust if project requires otherwise
                        return false;
                    default:
                        throw new JsonSerializationException($"Unexpected token parsing boolean: {reader.TokenType}.");
                }
            }

            public override void WriteJson(JsonWriter writer, bool value, JsonSerializer serializer)
            {
                // Write as "1"/"0" strings to match input style
                writer.WriteValue(value ? "1" : "0");
            }
        }
    }
}