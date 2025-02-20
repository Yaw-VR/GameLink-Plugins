using System;
using System.Runtime.InteropServices;

namespace WarplanesWW1Plugin
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    [Serializable]
    internal struct Packet
    {
        public float Pitch;
        public float Yaw;
        public float Heave;
        public float Roll;

        public float Sway;
        public float Surge;



    }
}
