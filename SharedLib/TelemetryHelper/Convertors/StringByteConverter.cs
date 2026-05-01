using System.Text;

namespace SharedLib.TelemetryHelper
{
    //public struct StringData
    //{
    //    public string Value;

    //    public override string ToString()
    //    {
    //        return Value;
    //    }
    //}

    internal class StringByteConverter : IByteConverter<string>
    {
        private readonly Encoding encoding;

        public StringByteConverter(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public string FromBytes(byte[] data)
        {
            return encoding.GetString(data);
        }

        public byte[] ToBytes(string data)
        {
            return data != null ? encoding.GetBytes(data) : [];
        }
    }

}
