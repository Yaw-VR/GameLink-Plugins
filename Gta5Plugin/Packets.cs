using System.Runtime.InteropServices;

namespace Gta5Plugin
{
    [StructLayout(LayoutKind.Explicit)]
    internal class Packets
    {

        //[FieldOffset(0)]
        //public float Time;

        //[FieldOffset(4)]
        //public float LapTime;

        //[FieldOffset(8)]
        //public float LapDistance;
        //[FieldOffset(12)]
        // public float Distance;
        //[FieldOffset(16)]
        //public float X;
        //[FieldOffset(20)]
        //public float Y;
        //[FieldOffset(24)]
        //public float Z;
        [FieldOffset(28)]
        public float Speed;
        //[FieldOffset(32)]
        //public float WorldSpeedX;
        //[FieldOffset(36)]
        //public float WorldSpeedY;
        //[FieldOffset(40)]
        //public float WorldSpeedZ;
        [FieldOffset(44)]
        public float Pitch;
        [FieldOffset(48)]
        public float Roll;
        [FieldOffset(52)]
        public float Vehicle_Heading;

        //[FieldOffset(56)]
        //public float XD;
        //[FieldOffset(60)]
        //public float YD;
        [FieldOffset(64)]
        public float PlayerHeading;
        //[FieldOffset(68)]
        //public float SuspensionPositionRearLeft;
        //[FieldOffset(72)]
        //public float SuspensionPositionRearRight;
        //[FieldOffset(76)]
        //public float SuspensionPositionFrontLeft;
        //[FieldOffset(80)]
        //public float SuspensionPositionFrontRight;
        //[FieldOffset(84)]
        //public float SuspensionVelocityRearLeft;
        //[FieldOffset(88)]
        //public float SuspensionVelocityRearRight;
        //[FieldOffset(92)]
        //public float SuspensionVelocityFrontLeft;
        //[FieldOffset(96)]
        //public float SuspensionVelocityFrontRight;
        //[FieldOffset(100)]
        //public float WheelSpeedReadLeft;
        //[FieldOffset(104)]
        //public float WheelSpeedRearRight;
        //[FieldOffset(108)]
        //public float WheelSpeedFrontLeft;
        //[FieldOffset(112)]
        //public float WheelSpeedFrontRight;
        //[FieldOffset(116)]
        //public float Throttle;
        //[FieldOffset(120)]
        //public float Steer;
        //[FieldOffset(124)]
        //public float Brake;
        //[FieldOffset(128)]
        //public float Clutch;
        //[FieldOffset(132)]
        //public float Gear;
        //[FieldOffset(136)]
        //public float LateralAcceleration;
        //[FieldOffset(140)]
        //public float LongitudinalAcceleration;
        // [FieldOffset(144)]
        //public float Speed;
        [FieldOffset(148)]
        public float RPM;
        //[FieldOffset(152)]
        //public float SliProNativeSupport;
        //[FieldOffset(156)]
        //public float RacePosition;
        //[FieldOffset(160)]
        //public float KersRemaining;
        //[FieldOffset(164)]
        //public float KersMaxLevel;
        //[FieldOffset(168)]
        //public float DrsStatus;
        //[FieldOffset(172)]
        //public float TractionControl;
        //[FieldOffset(176)]
        //public float AntiLock;
        //[FieldOffset(180)]
        //public float FuelRemaining;
        //[FieldOffset(184)]
        //public float FuelCapacity;
        [FieldOffset(188)]
        public float Shoot;
        [FieldOffset(192)]
        public float IsinWater;
        [FieldOffset(196)]
        public float IsinAir;
        [FieldOffset(200)]
        public float Brakepower;
        //[FieldOffset(204)]
        //public float BrakeTemperatureRearLeft;
        //[FieldOffset(208)]
        //public float BrakeTemperatureRearRight;
        //[FieldOffset(212)]
        //public float BrakeTemperatureFrontLeft;
        //[FieldOffset(216)]
        //public float BrakeTemperatureFrontRight;
        //[FieldOffset(220)]
        //public float WheelPressureRearLeft;
        //[FieldOffset(224)]
        //public float WheelPressureRearRight;
        //[FieldOffset(228)]
        //public float WheelPressureFrontLeft;
        //[FieldOffset(232)]
        //public float WheelPressureFrontRight;
        //[FieldOffset(236)]
        //public float CompletedLapsInRace;
        //[FieldOffset(240)]
        //public float TotalLapsInRace;
        //[FieldOffset(244)]
        //public float TrackLength;
        //[FieldOffset(248)]
        //public float PreviousLapTime;
        //[FieldOffset(252)]
        //public float MaxRpm;
        //[FieldOffset(256)]
        //public float IdleRpm;
        //[FieldOffset(260)]
        //public float MaxGears;
        //[FieldOffset(264)]
        //public float SessionType;
        //[FieldOffset(268)]
        //public float DrsAllowed;
        [FieldOffset(272)]
        public float Acceleration;
        //[FieldOffset(276)]
        //public float FIAFlags;

        public float SpeedInKmPerHour
        {
            get
            {
                return this.Speed * 3.6f;
            }
        }

        /*public bool IsSittingInPits {
            get {
                return Math.Abs(this.LapTime - 0f) < 1E-05f && Math.Abs(this.Speed - 0f) < 1E-05f;
            }
        }

        public bool IsInPitLane {
            get {
                return Math.Abs(this.LapTime - 0f) < 1E-05f;
            }
        }

        public string SessionTypeName {
            get {
                if (Math.Abs(this.SessionType - 9.5f) < 0.0001f) {
                    return "Race";
                }
                if (Math.Abs(this.SessionType - 10f) < 0.0001f) {
                    return "Time Trial";
                }
                if (Math.Abs(this.SessionType - 170f) < 0.0001f) {
                    return "Qualifying or Practice";
                }
                return "Other";
            }
        }
        */
        public float GetPropertyValueAt(int index)
        {
            var prop = this.GetType().GetProperties()[index];
            return (float)prop.GetValue(this, null);
        }
    }
}
