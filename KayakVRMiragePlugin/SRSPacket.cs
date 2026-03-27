using System.Runtime.InteropServices;

namespace KayakVRMiragePlugin
{
    /// <summary>
    /// SRS (SimRacingStudio) Motion Output Format - fixed-length binary UDP packet.
    /// Each field is a 32-bit IEEE 754 little-endian float.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
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
        public float Extra3;
        public float Extra4;
        public float Extra5;
        public float Extra6;
    }
}
