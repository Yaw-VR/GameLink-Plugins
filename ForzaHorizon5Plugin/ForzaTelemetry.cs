using System.Runtime.InteropServices;

namespace ForzaHorizon5Plugin
{
    [StructLayout(LayoutKind.Sequential)]
    internal class ForzaTelemetry
    {
        public uint IsRaceOn; // = 1 when race is on. = 0 when in menus/race stopped …

        public uint TimestampMS; //Can overflow to 0 eventually

        public float EngineMaxRpm;
        public float EngineIdleRpm;
        public float CurrentEngineRpm;

        public float AccelerationX; //In the car's local space; X = right, Y = up, Z = forward
        public float AccelerationY;
        public float AccelerationZ;

        public float VelocityX; //In the car's local space; X = right, Y = up, Z = forward
        public float VelocityY;
        public float VelocityZ;

        public float AngularVelocityX; //In the car's local space; X = pitch, Y = yaw, Z = roll
        public float AngularVelocityY;
        public float AngularVelocityZ;

        public float Yaw;
        public float Pitch;
        public float Roll;

        public float NormalizedSuspensionTravelFrontLeft; // Suspension travel normalized: 0.0f = max stretch; 1.0 = max compression
        public float NormalizedSuspensionTravelFrontRight;
        public float NormalizedSuspensionTravelRearLeft;
        public float NormalizedSuspensionTravelRearRight;

        public float TireSlipRatioFrontLeft; // Tire normalized slip ratio, = 0 means 100% grip and |ratio| > 1.0 means loss of grip.
        public float TireSlipRatioFrontRight;
        public float TireSlipRatioRearLeft;
        public float TireSlipRatioRearRight;

        public float WheelRotationSpeedFrontLeft; // Wheel rotation speed radians/sec.
        public float WheelRotationSpeedFrontRight;
        public float WheelRotationSpeedRearLeft;
        public float WheelRotationSpeedRearRight;

        public uint WheelOnRumbleStripFrontLeft; // = 1 when wheel is on rumble strip, = 0 when off.
        public uint WheelOnRumbleStripFrontRight;
        public uint WheelOnRumbleStripRearLeft;
        public uint WheelOnRumbleStripRearRight;

        public float WheelInPuddleDepthFrontLeft; // = from 0 to 1, where 1 is the deepest puddle
        public float WheelInPuddleDepthFrontRight;
        public float WheelInPuddleDepthRearLeft;
        public float WheelInPuddleDepthRearRight;

        public float SurfaceRumbleFrontLeft; // Non-dimensional surface rumble values passed to controller force feedback
        public float SurfaceRumbleFrontRight;
        public float SurfaceRumbleRearLeft;
        public float SurfaceRumbleRearRight;

        public float TireSlipAngleFrontLeft; // Tire normalized slip angle, = 0 means 100% grip and |angle| > 1.0 means loss of grip.
        public float TireSlipAngleFrontRight;
        public float TireSlipAngleRearLeft;
        public float TireSlipAngleRearRight;

        public float TireCombinedSlipFrontLeft; // Tire normalized combined slip, = 0 means 100% grip and |slip| > 1.0 means loss of grip.
        public float TireCombinedSlipFrontRight;
        public float TireCombinedSlipRearLeft;
        public float TireCombinedSlipRearRight;

        public float SuspensionTravelMetersFrontLeft; // Actual suspension travel in meters
        public float SuspensionTravelMetersFrontRight;
        public float SuspensionTravelMetersRearLeft;
        public float SuspensionTravelMetersRearRight;

        public uint CarOrdinal; //Unique ID of the car make/model
        public uint CarClass; //Between 0 (D -- worst cars) and 7 (X class -- best cars) inclusive
        public uint CarPerformanceIndex; //Between 100 (slowest car) and 999 (fastest car) inclusive
        public uint DrivetrainType; //Corresponds to EDrivetrainType; 0 = FWD, 1 = RWD, 2 = AWD
        public uint NumCylinders; //Number of cylinders in the engine


        public float speed;

    }
}
