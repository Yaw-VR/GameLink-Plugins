using System.Numerics;
using System.Runtime.InteropServices;

namespace YawVR_Game_Engine.Plugin
{

    [StructLayout(LayoutKind.Sequential)]
    internal struct Telemetry
    {
        public float Pitch;
        public float Yaw;
        public float Roll;

        public float AngularVelocityX;
        public float AngularVelocityY;
        public float AngularVelocityZ;

        public float cForce;

        public float VelocityX;
        public float VelocityY;
        public float VelocityZ;

        public float AccelX;
        public float AccelY;
        public float AccelZ;


        public float Speed;

        public float RPM;

        public int CurrentGear;

        public float TireFL;
        public float TireFR;
        public float TireBL;
        public float TireBR;

        public int WheelsOnTrack;
        public bool IsBoosting;
        public bool AllowDriving;
        public bool IsRacing;
        public bool IsEventOver;

        

    }



}
