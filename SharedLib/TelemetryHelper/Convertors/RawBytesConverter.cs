using System;

namespace SharedLib.TelemetryHelper
{
    internal class RawBytesConverter : IByteConverter<byte[]>
    {
        public byte[] FromBytes(byte[] data) => data ?? [];

        public byte[] ToBytes(byte[] data) => data ?? [];
    }
}
