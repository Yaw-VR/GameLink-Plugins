using System.Runtime.InteropServices;

namespace FlyDangerous
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FlyDangerousTelemetry
    {
        // Meta
        public uint flyDangerousTelemetryId;
        public uint packetId;

        // Game State
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public char[] gameVersion;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public char[] currentGameMode;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public char[] currentLevelName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public char[] currentMusicTrackName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public char[] currentShipName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public char[] playerName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] playerFlagIso;

        public int currentPlayerCount;

        // Instrument Data
        public float shipWorldPositionX;
        public float shipWorldPositionY;
        public float shipWorldPositionZ;

        public float shipAltitude;
        public float shipHeightFromGround;
        public float shipSpeed;
        public float accelerationMagnitudeNormalised;
        public float gForce;
        public float pitchPosition;
        public float rollPosition;
        public float yawPosition;
        public float throttlePosition;
        public float lateralHPosition;
        public float lateralVPosition;
        public float boostCapacitorPercent;
        public bool boostTimerReady;
        public bool boostChargeReady;
        public bool lightsActive;
        public bool underWater;
        public bool velocityLimiterActive;
        public bool vectorFlightAssistActive;
        public bool rotationalFlightAssistActive;
        public bool proximityWarning;
        public float proximityWarningSeconds;

        // Feedback Data
        public bool collisionThisFrame;
        public bool collisionStartedThisFrame;
        public float collisionImpactNormalised;
        public float collisionDirectionX;
        public float collisionDirectionY;
        public float collisionDirectionZ;

        public bool isBoostSpooling;
        public bool boostSpoolStartedThisFrame;
        public bool isBoostThrustActive;
        public bool boostThrustStartedThisFrame;
        public float boostSpoolTotalDurationSeconds;
        public float boostThrustTotalDurationSeconds;
        public float boostThrustProgressNormalised;
        public float shipShakeNormalised;

        // Motion Data
        public float currentLateralVelocityX;
        public float currentLateralVelocityY;
        public float currentLateralVelocityZ;

        public float currentLateralForceX;
        public float currentLateralForceY;
        public float currentLateralForceZ;

        public float currentAngularVelocityX;
        public float currentAngularVelocityY;
        public float currentAngularVelocityZ;

        public float currentAngularTorqueX;
        public float currentAngularTorqueY;
        public float currentAngularTorqueZ;

        public float currentLateralVelocityNormalisedX;
        public float currentLateralVelocityNormalisedY;
        public float currentLateralVelocityNormalisedZ;

        public float currentLateralForceNormalisedX;
        public float currentLateralForceNormalisedY;
        public float currentLateralForceNormalisedZ;

        public float currentAngularVelocityNormalisedX;
        public float currentAngularVelocityNormalisedY;
        public float currentAngularVelocityNormalisedZ;

        public float currentAngularTorqueNormalisedX;
        public float currentAngularTorqueNormalisedY;
        public float currentAngularTorqueNormalisedZ;

        public float shipWorldRotationEulerX;
        public float shipWorldRotationEulerY;
        public float shipWorldRotationEulerZ;

        public float maxSpeed;

        // String helpers for char[] handling
        public string GameVersion => new string(gameVersion).TrimEnd();
        public string CurrentGameMode => new string(currentGameMode).TrimEnd();
        public string CurrentLevelName => new string(currentLevelName).TrimEnd();
        public string CurrentMusicTrackName => new string(currentMusicTrackName).TrimEnd();
    }
}
