using System;

namespace KayakVRMiragePlugin
{
    /// <summary>
    /// SRS (SimRacingStudio) Motion Output Format.
    /// The packet has a variable-length metadata header followed by the motion block.
    /// The motion block is always the last 32 bytes of the packet (8 × IEEE 754 float).
    /// </summary>
    internal struct SRSPacket
    {
        public float Heave;
        public float Sway;
        public float Surge;
        public float Yaw;
        public float Pitch;
        public float Roll;
        public float Extra1;
        public float Extra2;

        private const int MotionBlockSize = 32; // 8 floats × 4 bytes

        public static SRSPacket FromBytes(byte[] data)
        {
            if (data.Length < MotionBlockSize)
                return default;

            int offset = data.Length - MotionBlockSize;
            return new SRSPacket
            {
                Heave  = BitConverter.ToSingle(data, offset +  0),
                Sway   = BitConverter.ToSingle(data, offset +  4),
                Surge  = BitConverter.ToSingle(data, offset +  8),
                Yaw    = BitConverter.ToSingle(data, offset + 12),
                Pitch  = BitConverter.ToSingle(data, offset + 16),
                Roll   = BitConverter.ToSingle(data, offset + 20),
                Extra1 = BitConverter.ToSingle(data, offset + 24),
                Extra2 = BitConverter.ToSingle(data, offset + 28),
            };
        }
    }
}
