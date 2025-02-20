using System;
using System.Runtime.InteropServices;

namespace RedOutPlugin
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    [Serializable]
    internal struct Packet
    {
        public float QuatX;
        public float QuatY;
        public float QuatZ;
        public float QuatW;

        public float AccWorldX;
        public float AccWorldY;
        public float AccWorldZ;

        public float AccX;
        public float AccY;
        public float AccZ;


    }
}
