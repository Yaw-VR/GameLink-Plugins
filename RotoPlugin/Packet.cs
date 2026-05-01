using System;
using System.Runtime.InteropServices;

namespace RotoVRPlugin
{
    [Serializable,StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct Telemetry
    {
        public int ActualAngle;

        public float PreciseAngle;

        public int TargetAngle;

        public int CappedTargetAngle;

        public float KalmanAngle;

        public float Delta;

        public int AntiJump;

        public float AngularVelocity;

        //public float AvgTargetAngle;


        public float SendHz;

        public float RecieveHz;

        public float LerpedHz;



        public int Direction;

        public int StopAngle;

        public int Power;

        public int MinPower;

        public float MaxPower;


    }
}
