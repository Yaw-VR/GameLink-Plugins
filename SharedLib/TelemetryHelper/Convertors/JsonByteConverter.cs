using Newtonsoft.Json;

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SharedLib.TelemetryHelper
{
    internal class NewtonsoftJsonConverter<T> : IByteConverter<T>
    {
        private readonly JsonSerializerSettings _options;

        public NewtonsoftJsonConverter() : this(new JsonSerializerSettings() )
        {
        }
        public NewtonsoftJsonConverter(JsonSerializerSettings options)
        {
            _options = new JsonSerializerSettings(options);
        }


        public byte[] ToBytes(T data)
        {
            string json = JsonConvert.SerializeObject(data, _options);
            return Encoding.UTF8.GetBytes(json);
        }
        public T FromBytes(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            var s = JsonConvert.DeserializeObject<T>(json, _options)!;
            return s;
        }
    }
    /// <summary>
    /// Converter json to byte array and vice versa.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class JsonByteConverter<T> : IByteConverter<T> //where T : struct
    {
        private readonly JsonSerializerOptions _options;

        public JsonByteConverter() : this(new JsonSerializerOptions() { PropertyNameCaseInsensitive = false })
        {
        }
        public JsonByteConverter(JsonSerializerOptions options)
        {
            _options = new JsonSerializerOptions(options) { PropertyNameCaseInsensitive = false };
        }


        public byte[] ToBytes(T data)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(data);
            return Encoding.UTF8.GetBytes(json);
        }
        public T FromBytes(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            var s = JsonConvert.DeserializeObject<T>(json)!;
            return s;
        }
    }

}
