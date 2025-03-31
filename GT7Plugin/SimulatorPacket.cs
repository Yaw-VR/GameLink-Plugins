using System.Diagnostics;
using System.Net;
using System.Numerics;


// credit. Nenkai - PDTools https://github.com/Nenkai/PDTools
namespace GT7Plugin
{
    public enum SimInterfacePacketType
    {
        PacketType1,

        // Both of these were added in GT7 1.42.
        // Initial signs of expansions can be seen in 1.40 (possibly earlier even?) - 3 switch cases (+ default)
        // can already be seen, all using 'A' and the same amount of bytes.

        /// <summary>
        /// GT7 >= 1.42
        /// </summary>
        PacketType2,

        /// <summary>
        /// GT7 >= 1.42
        /// </summary>
        PacketType3
    }

    /// <summary>
    /// Packet from the GT Engine Simulation.
    /// </summary>
    public class SimulatorPacket
    {
        /// <summary>
        /// Position on the track. Track units are in meters.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Velocity in track units (which are meters) for each axis.
        /// </summary>
        public Vector3 Velocity { get; set; }

        /// <summary>
        /// Rotation as a quaternion.
        /// </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>
        /// How fast the car turns around axes. (In radians/second, -1 to 1).
        /// </summary>
        public Vector3 AngularVelocity { get; set; }

        /// <summary>
        /// Body height.
        /// </summary>
        public float BodyHeight { get; set; }

        /// <summary>
        /// Engine revolutions per minute.
        /// </summary>
        public float EngineRPM { get; set; }

        /// <summary>
        /// Gas level for the current car (in liters, from 0 to <see cref="GasCapacity"/>).
        /// <para> Note: This may change from 0 when regenerative braking with electric cars, check accordingly with <see cref="GasCapacity"/>. </para>
        /// </summary>
        public float GasLevel { get; set; }

        /// <summary>
        /// Max gas capacity for the current car.
        /// Will be 100 for most cars, 5 for karts, 0 for electric cars
        /// </summary>
        public float GasCapacity { get; set; }

        /// <summary>
        /// Current speed in meters per second. <see cref="MetersPerSecond * 3.6"/> to get it in KPH.
        /// </summary>
        public float MetersPerSecond { get; set; }

        /// <summary>
        /// Value below 1.0 is below 0 ingame, so 2.0 = 1 x 100kPa
        /// </summary>
        public float TurboBoost { get; set; }

        /// <summary>
        /// Oil Pressure (in Bars)
        /// </summary>
        public float OilPressure { get; set; }

        /// <summary>
        /// Games will always send 85.
        /// </summary>
        public float WaterTemperature { get; set; }

        /// <summary>
        /// Games will always send 110.
        /// </summary>
        public float OilTemperature { get; set; }

        /// <summary>
        /// Front Left Tire - Surface Temperature (in °C)
        /// </summary>
        public float TireFL_SurfaceTemperature { get; set; }

        /// <summary>
        /// Front Right - Surface Temperature (in °C)
        /// </summary>
        public float TireFR_SurfaceTemperature { get; set; }

        /// <summary>
        /// Rear Left - Surface Temperature (in °C)
        /// </summary>
        public float TireRL_SurfaceTemperature { get; set; }

        /// <summary>
        /// Rear Right - Surface Temperature (in °C)
        /// </summary>
        public float TireRR_SurfaceTemperature { get; set; }

        /// <summary>
        /// Id of the packet for proper ordering.
        /// </summary>
        public int PacketId { get; set; }

        /// <summary>
        /// Current lap count.
        /// </summary>
        public short LapCount { get; set; }

        /// <summary>
        /// Laps to finish.
        /// </summary>
        public short LapsInRace { get; set; }

        /// <summary>
        /// Best Lap Time. 
        /// <para>Defaults to -1 millisecond when not set.</para>
        /// </summary>
        public TimeSpan BestLapTime { get; set; }

        /// <summary>
        /// Last Lap Time.
        /// <para>Defaults to -1 millisecond when not set.</para>
        /// </summary>
        public TimeSpan LastLapTime { get; set; }

        /// <summary>
        /// Current time of day on the track.
        /// </summary>
        public TimeSpan TimeOfDayProgression { get; set; }

        /// <summary>
        /// Position of the car before the race has started.
        /// <para>Will be -1 once the race is started.</para>
        /// </summary>
        public short PreRaceStartPositionOrQualiPos { get; set; }

        /// <summary>
        /// Number of cars in the race before the race has started.
        /// <para>Will be -1 once the race is started.</para>
        /// </summary>
        public short NumCarsAtPreRace { get; set; }

        /// <summary>
        /// Minimum RPM to which the rev limiter shows an alert.
        /// </summary>
        public short MinAlertRPM { get; set; }

        /// <summary>
        /// Maximum RPM to the rev limiter alert.
        /// </summary>
        public short MaxAlertRPM { get; set; }

        /// <summary>
        /// Possible max speed achievable using the current transmission settings.
        /// <para> Will change depending on transmission settings.</para>
        /// </summary>
        public short CalculatedMaxSpeed { get; set; }

        /// <summary>
        /// Packet flags.
        /// </summary>
        public SimulatorFlags Flags { get; set; }

        /// <summary>
        /// Current Gear for the car.
        /// <para> This value will never be more than 15 (4 bits).</para>
        /// </summary>
        public byte CurrentGear { get; set; }

        /// <summary>
        /// (Assist) Suggested Gear to downshift to. 
        /// <para> This value will never be more than 15 (4 bits), All bits on (aka 15) implies there is no current suggested gear.</para>
        /// </summary>
        public byte SuggestedGear { get; set; }

        /// <summary>
        /// Throttle (0-255)
        /// </summary>
        public byte Throttle { get; set; }

        /// <summary>
        /// Brake Pedal (0-255)
        /// </summary>
        public byte Brake { get; set; }

        public byte Empty_0x93 { get; set; }

        public Vector3 RoadPlane { get; set; }

        public float RoadPlaneDistance { get; set; }

        /// <summary>
        /// Front Left Wheel - Revolutions Per Second (in Radians)
        /// </summary>
        public float WheelFL_RevPerSecond { get; set; }

        /// <summary>
        /// Front Right Wheel - Revolutions Per Second (in Radians)
        /// </summary>
        public float WheelFR_RevPerSecond { get; set; }

        /// <summary>
        /// Rear Left Wheel - Revolutions Per Second (in Radians)
        /// </summary>
        public float WheelRL_RevPerSecond { get; set; }

        /// <summary>
        /// Rear Right Wheel - Revolutions Per Second (in Radians)
        /// </summary>
        public float WheelRR_RevPerSecond { get; set; }

        /// <summary>
        /// Front Left Tire - Tire Radius (in Meters)
        /// </summary>
        public float TireFL_TireRadius { get; set; }

        /// <summary>
        /// Front Right Tire - Tire Radius (in Meters)
        /// </summary>
        public float TireFR_TireRadius { get; set; }

        /// <summary>
        /// Rear Left Tire - Tire Radius (in Meters)
        /// </summary>
        public float TireRL_TireRadius { get; set; }

        /// <summary>
        /// Rear Right Tire - Tire Radius (in Meters)
        /// </summary>
        public float TireRR_TireRadius { get; set; }

        /// <summary>
        /// Front Left Tire - Suspension Height
        /// </summary>
        public float TireFL_SusHeight { get; set; }

        /// <summary>
        /// Front Right Tire - Suspension Height
        /// </summary>
        public float TireFR_SusHeight { get; set; }

        /// <summary>
        /// Rear Left Tire - Suspension Height
        /// </summary>
        public float TireRL_SusHeight { get; set; }

        /// <summary>
        /// Rear Right Tire - Suspension Height
        /// </summary>
        public float TireRR_SusHeight { get; set; }

        /// <summary>
        /// 0.0 to 1.0
        /// </summary>
        public float ClutchPedal { get; set; }

        /// <summary>
        /// 0.0 to 1.0
        /// </summary>
        public float ClutchEngagement { get; set; }

        /// <summary>
        /// Basically same as engine rpm when in gear and the clutch pedal is not depressed.
        /// </summary>
        public float RPMFromClutchToGearbox { get; set; }

        /// <summary>
        /// Top Speed (as a Gear Ratio value)
        /// </summary>
        public float TransmissionTopSpeed { get; set; }

        /// <summary>
        /// Gear ratios for the car. Up to 7.
        /// </summary>
        public float[] GearRatios { get; set; } = new float[7];

        /// <summary>
        /// Internal code that identifies the car.
        /// <para>This value may be overriden if using a car that uses 9 or more gears (oversight).</para>
        /// </summary>
        public int CarCode { get; set; }

        /// <summary>
        /// In radians. GT7 and Heartbeat 'B' or '~' only.
        /// </summary>
        public float? WheelRotation { get; set; }

        /// <summary>
        /// GT7 and Heartbeat 'B' or '~' only.
        /// </summary>
        public float? FillerFloatFB { get; set; }

        /// <summary>
        /// GT7 and Heartbeat 'B' or '~' only.
        /// </summary>
        public float? Sway { get; set; }

        /// <summary>
        /// GT7 and Heartbeat 'B' or '~' only.
        /// </summary>
        public float? Heave { get; set; }

        /// <summary>
        /// GT7 and Heartbeat 'B' or '~' only.
        /// </summary>
        public float? Surge { get; set; }

        public byte? Unk1 { get; set; }
        public byte? Unk2 { get; set; }
        public byte? Unk3 { get; set; } // 4 = electric
        public byte? NoGasConsumption { get; set; }
        public Vector4? Unk5 { get; set; }
        public float? EnergyRecovery { get; set; }
        public float? Unk7 { get; set; }

        public void Read(Span<byte> data)
        {
            var sr = new SpanReader(data);
            int magic = sr.ReadInt32();

            Position = new Vector3(sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle());
            Velocity = new Vector3(sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle());
            Rotation = new Quaternion(sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle());
            AngularVelocity = new Vector3(sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle());
            BodyHeight = sr.ReadSingle();
            EngineRPM = sr.ReadSingle();
            sr.Position += sizeof(int); // Skip IV
            GasLevel = sr.ReadSingle();
            GasCapacity = sr.ReadSingle();
            MetersPerSecond = sr.ReadSingle();
            TurboBoost = sr.ReadSingle();
            OilPressure = sr.ReadSingle();
            WaterTemperature = sr.ReadSingle();
            OilTemperature = sr.ReadSingle();
            TireFL_SurfaceTemperature = sr.ReadSingle();
            TireFR_SurfaceTemperature = sr.ReadSingle();
            TireRL_SurfaceTemperature = sr.ReadSingle();
            TireRR_SurfaceTemperature = sr.ReadSingle();
            PacketId = sr.ReadInt32();
            LapCount = sr.ReadInt16();
            LapsInRace = sr.ReadInt16();
            BestLapTime = TimeSpan.FromMilliseconds(sr.ReadInt32());
            LastLapTime = TimeSpan.FromMilliseconds(sr.ReadInt32());
            TimeOfDayProgression = TimeSpan.FromMilliseconds(sr.ReadInt32());
            PreRaceStartPositionOrQualiPos = sr.ReadInt16();
            NumCarsAtPreRace = sr.ReadInt16();
            MinAlertRPM = sr.ReadInt16();
            MaxAlertRPM = sr.ReadInt16();
            CalculatedMaxSpeed = sr.ReadInt16();
            Flags = (SimulatorFlags)sr.ReadInt16();

            int bits = sr.ReadByte();
            CurrentGear = (byte)(bits & 0b1111); // 4 bits
            SuggestedGear = (byte)(bits >> 4); // Also 4 bits

            Throttle = sr.ReadByte();
            Brake = sr.ReadByte();
            Empty_0x93 = sr.ReadByte();

            RoadPlane = new Vector3(sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle());
            RoadPlaneDistance = sr.ReadSingle();

            WheelFL_RevPerSecond = sr.ReadSingle();
            WheelFR_RevPerSecond = sr.ReadSingle();
            WheelRL_RevPerSecond = sr.ReadSingle();
            WheelRR_RevPerSecond = sr.ReadSingle();
            TireFL_TireRadius = sr.ReadSingle();
            TireFR_TireRadius = sr.ReadSingle();
            TireRL_TireRadius = sr.ReadSingle();
            TireRR_TireRadius = sr.ReadSingle();
            TireFL_SusHeight = sr.ReadSingle();
            TireFR_SusHeight = sr.ReadSingle();
            TireRL_SusHeight = sr.ReadSingle();
            TireRR_SusHeight = sr.ReadSingle();

            sr.Position += sizeof(int) * 8; // Seems to be reserved - server does not set that

            ClutchPedal = sr.ReadSingle();
            ClutchEngagement = sr.ReadSingle();
            RPMFromClutchToGearbox = sr.ReadSingle();

            TransmissionTopSpeed = sr.ReadSingle();

            // Always read as a fixed 7 gears
            for (var i = 0; i < 7; i++)
                GearRatios[i] = sr.ReadSingle();

            // Normally this one is not set at all. The game memcpy's the gear ratios without bound checking
            // The LC500 which has 10 gears even overrides the car code 😂
            float empty_or_gearRatio8 = sr.ReadSingle();

            CarCode = sr.ReadInt32();

            if (data.Length >= 0x13C)
            {
                WheelRotation = sr.ReadSingle();
                FillerFloatFB = sr.ReadSingle();
                Sway = sr.ReadSingle();
                Heave = sr.ReadSingle();
                Surge = sr.ReadSingle();
            }

            if (data.Length >= 0x158)
            {
                Unk1 = sr.ReadByte();
                Unk2 = sr.ReadByte();
                Unk3 = sr.ReadByte();
                NoGasConsumption = sr.ReadByte();
                Unk5 = new Vector4(sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle());
                EnergyRecovery = sr.ReadSingle();
                Unk7 = sr.ReadSingle();
            }
        }

    }

    /// <summary>
    /// Flags/States of the simulation.
    /// </summary>
    [Flags]
    public enum SimulatorFlags : short
    {
        None = 0,

        /// <summary>
        /// The car is on the track or paddock, with data available.
        /// </summary>
        CarOnTrack = 1 << 0,

        /// <summary>
        /// The game's simulation is paused. 
        /// Note: The simulation will not be paused while in the pause menu in online modes.
        /// </summary>
        Paused = 1 << 1,

        /// <summary>
        /// Track or car is currently being loaded onto the track.
        /// </summary>
        LoadingOrProcessing = 1 << 2,

        /// <summary>
        /// Needs more investigation
        /// </summary>
        InGear = 1 << 3,

        /// <summary>
        /// Current car has a Turbo.
        /// </summary>
        HasTurbo = 1 << 4,

        /// <summary>
        /// Rev Limiting is active.
        /// </summary>
        RevLimiterBlinkAlertActive = 1 << 5,

        /// <summary>
        /// Hand Brake is active.
        /// </summary>
        HandBrakeActive = 1 << 6,

        /// <summary>
        /// Lights are active.
        /// </summary>
        LightsActive = 1 << 7,

        /// <summary>
        /// High Beams are turned on.
        /// </summary>
        HighBeamActive = 1 << 8,

        /// <summary>
        /// Low Beams are turned on.
        /// </summary>
        LowBeamActive = 1 << 9,

        /// <summary>
        /// ASM is active.
        /// </summary>
        ASMActive = 1 << 10,

        /// <summary>
        /// Traction Control is active.
        /// </summary>
        TCSActive = 1 << 11,
    }
}