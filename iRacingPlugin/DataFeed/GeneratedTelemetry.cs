
// This file is part of iRacingSDK.
//
// Copyright 2014 Dean Netherton
// https://github.com/vipoo/iRacingSDK.Net
//
// iRacingSDK is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// iRacingSDK is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with iRacingSDK.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;

namespace iRacingSDK
{
    public partial class Telemetry : Dictionary<string, object>
    {
        public SessionData SessionData { get; set; }

        /// <summary>
        /// Seconds since session start
        /// </summary>
        public Double SessionTime => ContainsKey("SessionTime") ? (Double)this["SessionTime"] : 0;

        /// <summary>
        /// Session number
        /// </summary>
        public Int32 SessionNum => ContainsKey("SessionNum") ? (Int32)this["SessionNum"] : 0;

        /// <summary>
        /// Session state
        /// </summary>
        public SessionState SessionState => ContainsKey("SessionState") ? (SessionState)this["SessionState"] : SessionState.Invalid;

        /// <summary>
        /// Session ID
        /// </summary>
        public Int32 SessionUniqueID => ContainsKey("SessionUniqueID") ? (Int32)this["SessionUniqueID"] : 0;

        /// <summary>
        /// Session flags
        /// </summary>
        public SessionFlags SessionFlags => ContainsKey("SessionFlags") ? (SessionFlags)(int)this["SessionFlags"] : 0;

        /// <summary>
        /// Seconds left till session ends
        /// </summary>
        public Double SessionTimeRemain => ContainsKey("SessionTimeRemain") ? (Double)this["SessionTimeRemain"] : 0;

        /// <summary>
        /// Old laps left till session ends use SessionLapsRemainEx
        /// </summary>
        public Int32 SessionLapsRemain => ContainsKey("SessionLapsRemain") ? (Int32)this["SessionLapsRemain"] : 0;

        /// <summary>
        /// New improved laps left till session ends
        /// </summary>
        public Int32 SessionLapsRemainEx => ContainsKey("SessionLapsRemainEx") ? (Int32)this["SessionLapsRemainEx"] : 0;

        /// <summary>
        /// The car index of the current person speaking on the radio
        /// </summary>
        public Int32 RadioTransmitCarIdx => ContainsKey("RadioTransmitCarIdx") ? (Int32)this["RadioTransmitCarIdx"] : 0;

        /// <summary>
        /// The radio index of the current person speaking on the radio
        /// </summary>
        public Int32 RadioTransmitRadioIdx => ContainsKey("RadioTransmitRadioIdx") ? (Int32)this["RadioTransmitRadioIdx"] : 0;

        /// <summary>
        /// The frequency index of the current person speaking on the radio
        /// </summary>
        public Int32 RadioTransmitFrequencyIdx => ContainsKey("RadioTransmitFrequencyIdx") ? (Int32)this["RadioTransmitFrequencyIdx"] : 0;

        /// <summary>
        /// Default units for the user interface 0 = english 1 = metric
        /// </summary>
        public DisplayUnits DisplayUnits => ContainsKey("DisplayUnits") ? (DisplayUnits)(Int32)this["DisplayUnits"] : 0;

        /// <summary>
        /// Driver activated flag
        /// </summary>
        public Boolean DriverMarker => ContainsKey("DriverMarker") && (Boolean)this["DriverMarker"];

        /// <summary>
        /// 1=Car on track physics running with player in car
        /// </summary>
        public Boolean IsOnTrack => ContainsKey("IsOnTrack") && (Boolean)this["IsOnTrack"];

        /// <summary>
        /// 0=replay not playing  1=replay playing
        /// </summary>
        public Boolean IsReplayPlaying => ContainsKey("IsReplayPlaying") && (Boolean)this["IsReplayPlaying"];

        /// <summary>
        /// Integer replay frame number (60 per second)
        /// </summary>
        public Int32 ReplayFrameNum => ContainsKey("ReplayFrameNum") ? (Int32)this["ReplayFrameNum"] : 0;

        /// <summary>
        /// Integer replay frame number from end of tape
        /// </summary>
        public Int32 ReplayFrameNumEnd => ContainsKey("ReplayFrameNumEnd") ? (Int32)this["ReplayFrameNumEnd"] : 0;

        /// <summary>
        /// 0=disk based telemetry turned off  1=turned on
        /// </summary>
        public Boolean IsDiskLoggingEnabled => ContainsKey("IsDiskLoggingEnabled") && (Boolean)this["IsDiskLoggingEnabled"];

        /// <summary>
        /// 0=disk based telemetry file not being written  1=being written
        /// </summary>
        public Boolean IsDiskLoggingActive => ContainsKey("IsDiskLoggingActive") && (Boolean)this["IsDiskLoggingActive"];

        /// <summary>
        /// Average frames per second
        /// </summary>
        public Single FrameRate => ContainsKey("FrameRate") ? (Single)this["FrameRate"] : 0;

        /// <summary>
        /// Percent of available tim bg thread took with a 1 sec avg
        /// </summary>
        public Single CpuUsageBG => ContainsKey("CpuUsageBG") ? (Single)this["CpuUsageBG"] : 0;

        /// <summary>
        /// Players position in race
        /// </summary>
        public Int32 PlayerCarPosition => ContainsKey("PlayerCarPosition") ? (Int32)this["PlayerCarPosition"] : 0;

        /// <summary>
        /// Players class position in race
        /// </summary>
        public Int32 PlayerCarClassPosition => ContainsKey("PlayerCarClassPosition") ? (Int32)this["PlayerCarClassPosition"] : 0;

        /// <summary>
        /// Laps started by car index
        /// </summary>
        public Int32[] CarIdxLap => ContainsKey("CarIdxLap") ? (Int32[])this["CarIdxLap"] : [];

        /// <summary>
        /// Laps completed by car index
        /// </summary>
        public Int32[] CarIdxLapCompleted => ContainsKey("CarIdxLapCompleted") ? (Int32[])this["CarIdxLapCompleted"] : [];

        /// <summary>
        /// Percentage distance around lap by car index
        /// </summary>
        public Single[] CarIdxLapDistPct => ContainsKey("CarIdxLapDistPct") ? (Single[])this["CarIdxLapDistPct"] : [];

        /// <summary>
        /// Track surface type by car index
        /// </summary>
        public TrackLocation[] CarIdxTrackSurface => ContainsKey("CarIdxTrackSurface") ? (TrackLocation[])this["CarIdxTrackSurface"] : [];

        /// <summary>
        /// On pit road between the cones by car index
        /// </summary>
        public Boolean[] CarIdxOnPitRoad => ContainsKey("CarIdxOnPitRoad") ? (Boolean[])this["CarIdxOnPitRoad"] : [];

        /// <summary>
        /// Cars position in race by car index
        /// </summary>
        public Int32[] CarIdxPosition => ContainsKey("CarIdxPosition") ? (Int32[])this["CarIdxPosition"] : [];

        /// <summary>
        /// Cars class position in race by car index
        /// </summary>
        public Int32[] CarIdxClassPosition => ContainsKey("CarIdxClassPosition") ? (Int32[])this["CarIdxClassPosition"] : [];

        /// <summary>
        /// Race time behind leader or fastest lap time otherwise
        /// </summary>
        public Single[] CarIdxF2Time => ContainsKey("CarIdxF2Time") ? (Single[])this["CarIdxF2Time"] : [];

        /// <summary>
        /// Estimated time to reach current location on track
        /// </summary>
        public Single[] CarIdxEstTime => ContainsKey("CarIdxEstTime") ? (Single[])this["CarIdxEstTime"] : [];

        /// <summary>
        /// Is the player car on pit road between the cones
        /// </summary>
        public Boolean OnPitRoad => ContainsKey("OnPitRoad") && (Boolean)this["OnPitRoad"];

        /// <summary>
        /// Steering wheel angle by car index
        /// </summary>
        public Single[] CarIdxSteer => ContainsKey("CarIdxSteer") ? (Single[])this["CarIdxSteer"] : [];

        /// <summary>
        /// Engine rpm by car index
        /// </summary>
        public Single[] CarIdxRPM => ContainsKey("CarIdxRPM") ? (Single[])this["CarIdxRPM"] : [];

        /// <summary>
        /// -1=reverse  0=neutral  1..n=current gear by car index
        /// </summary>
        public Int32[] CarIdxGear => ContainsKey("CarIdxGear") ? (Int32[])this["CarIdxGear"] : [];

        /// <summary>
        /// Steering wheel angle
        /// </summary>
        public Single SteeringWheelAngle => ContainsKey("SteeringWheelAngle") ? (Single)this["SteeringWheelAngle"] : 0;

        /// <summary>
        /// 0=off throttle to 1=full throttle
        /// </summary>
        public Single Throttle => ContainsKey("Throttle") ? (Single)this["Throttle"] : 0;

        /// <summary>
        /// 0=brake released to 1=max pedal force
        /// </summary>
        public Single Brake => ContainsKey("Brake") ? (Single)this["Brake"] : 0;

        /// <summary>
        /// 0=disengaged to 1=fully engaged
        /// </summary>
        public Single Clutch => ContainsKey("Clutch") ? (Single)this["Clutch"] : 0;

        /// <summary>
        /// -1=reverse  0=neutral  1..n=current gear
        /// </summary>
        public Int32 Gear => ContainsKey("Gear") ? (Int32)this["Gear"] : 0;

        /// <summary>
        /// Engine rpm
        /// </summary>
        public Single RPM => ContainsKey("RPM") ? (Single)this["RPM"] : 0;

        /// <summary>
        /// Laps started count
        /// </summary>
        public Int32 Lap => ContainsKey("Lap") ? (Int32)this["Lap"] : 0;

        /// <summary>
        /// Laps completed count
        /// </summary>
        public Int32 LapCompleted => ContainsKey("LapCompleted") ? (Int32)this["LapCompleted"] : 0;

        /// <summary>
        /// Meters traveled from S/F this lap
        /// </summary>
        public Single LapDist => ContainsKey("LapDist") ? (Single)this["LapDist"] : 0;

        /// <summary>
        /// Percentage distance around lap
        /// </summary>
        public Single LapDistPct => ContainsKey("LapDistPct") ? (Single)this["LapDistPct"] : 0;

        /// <summary>
        /// Laps completed in race
        /// </summary>
        public Int32 RaceLaps => ContainsKey("RaceLaps") ? (Int32)this["RaceLaps"] : 0;

        /// <summary>
        /// Players best lap number
        /// </summary>
        public Int32 LapBestLap => ContainsKey("LapBestLap") ? (Int32)this["LapBestLap"] : 0;

        /// <summary>
        /// Players best lap time
        /// </summary>
        public Single LapBestLapTime => ContainsKey("LapBestLapTime") ? (Single)this["LapBestLapTime"] : 0;

        /// <summary>
        /// Players last lap time
        /// </summary>
        public Single LapLastLapTime => ContainsKey("LapLastLapTime") ? (Single)this["LapLastLapTime"] : 0;

        /// <summary>
        /// Estimate of players current lap time as shown in F3 box
        /// </summary>
        public Single LapCurrentLapTime => ContainsKey("LapCurrentLapTime") ? (Single)this["LapCurrentLapTime"] : 0;

        /// <summary>
        /// Player num consecutive clean laps completed for N average
        /// </summary>
        public Int32 LapLasNLapSeq => ContainsKey("LapLasNLapSeq") ? (Int32)this["LapLasNLapSeq"] : 0;

        /// <summary>
        /// Player last N average lap time
        /// </summary>
        public Single LapLastNLapTime => ContainsKey("LapLastNLapTime") ? (Single)this["LapLastNLapTime"] : 0;

        /// <summary>
        /// Player last lap in best N average lap time
        /// </summary>
        public Int32 LapBestNLapLap => ContainsKey("LapBestNLapLap") ? (Int32)this["LapBestNLapLap"] : 0;

        /// <summary>
        /// Player best N average lap time
        /// </summary>
        public Single LapBestNLapTime => ContainsKey("LapBestNLapTime") ? (Single)this["LapBestNLapTime"] : 0;

        /// <summary>
        /// Delta time for best lap
        /// </summary>
        public Single LapDeltaToBestLap => ContainsKey("LapDeltaToBestLap") ? (Single)this["LapDeltaToBestLap"] : 0;

        /// <summary>
        /// Rate of change of delta time for best lap
        /// </summary>
        public Single LapDeltaToBestLap_DD => ContainsKey("LapDeltaToBestLap_DD") ? (Single)this["LapDeltaToBestLap_DD"] : 0;

        /// <summary>
        /// Delta time for best lap is valid
        /// </summary>
        public Boolean LapDeltaToBestLap_OK => ContainsKey("LapDeltaToBestLap_OK") && (Boolean)this["LapDeltaToBestLap_OK"];

        /// <summary>
        /// Delta time for optimal lap
        /// </summary>
        public Single LapDeltaToOptimalLap => ContainsKey("LapDeltaToOptimalLap") ? (Single)this["LapDeltaToOptimalLap"] : 0;

        /// <summary>
        /// Rate of change of delta time for optimal lap
        /// </summary>
        public Single LapDeltaToOptimalLap_DD => ContainsKey("LapDeltaToOptimalLap_DD") ? (Single)this["LapDeltaToOptimalLap_DD"] : 0;

        /// <summary>
        /// Delta time for optimal lap is valid
        /// </summary>
        public Boolean LapDeltaToOptimalLap_OK => ContainsKey("LapDeltaToOptimalLap_OK") && (Boolean)this["LapDeltaToOptimalLap_OK"];

        /// <summary>
        /// Delta time for session best lap
        /// </summary>
        public Single LapDeltaToSessionBestLap => ContainsKey("LapDeltaToSessionBestLap") ? (Single)this["LapDeltaToSessionBestLap"] : 0;

        /// <summary>
        /// Rate of change of delta time for session best lap
        /// </summary>
        public Single LapDeltaToSessionBestLap_DD => ContainsKey("LapDeltaToSessionBestLap_DD") ? (Single)this["LapDeltaToSessionBestLap_DD"] : 0;

        /// <summary>
        /// Delta time for session best lap is valid
        /// </summary>
        public Boolean LapDeltaToSessionBestLap_OK => ContainsKey("LapDeltaToSessionBestLap_OK") && (Boolean)this["LapDeltaToSessionBestLap_OK"];

        /// <summary>
        /// Delta time for session optimal lap
        /// </summary>
        public Single LapDeltaToSessionOptimalLap => ContainsKey("LapDeltaToSessionOptimalLap") ? (Single)this["LapDeltaToSessionOptimalLap"] : 0;

        /// <summary>
        /// Rate of change of delta time for session optimal lap
        /// </summary>
        public Single LapDeltaToSessionOptimalLap_DD => ContainsKey("LapDeltaToSessionOptimalLap_DD") ? (Single)this["LapDeltaToSessionOptimalLap_DD"] : 0;

        /// <summary>
        /// Delta time for session optimal lap is valid
        /// </summary>
        public Boolean LapDeltaToSessionOptimalLap_OK => ContainsKey("LapDeltaToSessionOptimalLap_OK") && (Boolean)this["LapDeltaToSessionOptimalLap_OK"];

        /// <summary>
        /// Delta time for session last lap
        /// </summary>
        public Single LapDeltaToSessionLastlLap => ContainsKey("LapDeltaToSessionLastlLap") ? (Single)this["LapDeltaToSessionLastlLap"] : 0;

        /// <summary>
        /// Rate of change of delta time for session last lap
        /// </summary>
        public Single LapDeltaToSessionLastlLap_DD => ContainsKey("LapDeltaToSessionLastlLap_DD") ? (Single)this["LapDeltaToSessionLastlLap_DD"] : 0;

        /// <summary>
        /// Delta time for session last lap is valid
        /// </summary>
        public Boolean LapDeltaToSessionLastlLap_OK => ContainsKey("LapDeltaToSessionLastlLap_OK") && (Boolean)this["LapDeltaToSessionLastlLap_OK"];

        /// <summary>
        /// Longitudinal acceleration (including gravity)
        /// </summary>
        public Single LongAccel => ContainsKey("LongAccel") ? (Single)this["LongAccel"] : 0;

        /// <summary>
        /// Lateral acceleration (including gravity)
        /// </summary>
        public Single LatAccel => ContainsKey("LatAccel") ? (Single)this["LatAccel"] : 0;

        /// <summary>
        /// Vertical acceleration (including gravity)
        /// </summary>
        public Single VertAccel => ContainsKey("VertAccel") ? (Single)this["VertAccel"] : 0;

        /// <summary>
        /// Roll rate
        /// </summary>
        public Single RollRate => ContainsKey("RollRate") ? (Single)this["RollRate"] : 0;

        /// <summary>
        /// Pitch rate
        /// </summary>
        public Single PitchRate => ContainsKey("PitchRate") ? (Single)this["PitchRate"] : 0;

        /// <summary>
        /// Yaw rate
        /// </summary>
        public Single YawRate => ContainsKey("YawRate") ? (Single)this["YawRate"] : 0;

        /// <summary>
        /// GPS vehicle speed
        /// </summary>
        public Single Speed => ContainsKey("Speed") ? (Single)this["Speed"] : 0;

        /// <summary>
        /// X velocity
        /// </summary>
        public Single VelocityX => ContainsKey("VelocityX") ? (Single)this["VelocityX"] : 0;

        /// <summary>
        /// Y velocity
        /// </summary>
        public Single VelocityY => ContainsKey("VelocityY") ? (Single)this["VelocityY"] : 0;

        /// <summary>
        /// Z velocity
        /// </summary>
        public Single VelocityZ => ContainsKey("VelocityZ") ? (Single)this["VelocityZ"] : 0;

        /// <summary>
        /// Yaw orientation
        /// </summary>
        public Single Yaw => ContainsKey("Yaw") ? (Single)this["Yaw"] : 0;

        /// <summary>
        /// Yaw orientation relative to north
        /// </summary>
        public Single YawNorth => ContainsKey("YawNorth") ? (Single)this["YawNorth"] : 0;

        /// <summary>
        /// Pitch orientation
        /// </summary>
        public Single Pitch => ContainsKey("Pitch") ? (Single)this["Pitch"] : 0;

        /// <summary>
        /// Roll orientation
        /// </summary>
        public Single Roll => ContainsKey("Roll") ? (Single)this["Roll"] : 0;

        /// <summary>
        /// Indicate action the reset key will take 0 enter 1 exit 2 reset
        /// </summary>
        public Int32 EnterExitReset => ContainsKey("EnterExitReset") ? (Int32)this["EnterExitReset"] : 0;

        /// <summary>
        /// Temperature of track at start/finish line
        /// </summary>
        public Single TrackTemp => ContainsKey("TrackTemp") ? (Single)this["TrackTemp"] : 0;

        /// <summary>
        /// Temperature of track measured by crew around track
        /// </summary>
        public Single TrackTempCrew => ContainsKey("TrackTempCrew") ? (Single)this["TrackTempCrew"] : 0;

        /// <summary>
        /// Temperature of air at start/finish line
        /// </summary>
        public Single AirTemp => ContainsKey("AirTemp") ? (Single)this["AirTemp"] : 0;

        /// <summary>
        /// Weather type (0=constant  1=dynamic)
        /// </summary>
        public WeatherType WeatherType => ContainsKey("WeatherType") ? (WeatherType)(Int32)this["WeatherType"] : WeatherType.Constant;

        /// <summary>
        /// Skies (0=clear/1=p cloudy/2=m cloudy/3=overcast)
        /// </summary>
        public Skies Skies => ContainsKey("Skies") ? (Skies)(Int32)this["Skies"] : Skies.Clear;

        /// <summary>
        /// Density of air at start/finish line
        /// </summary>
        public Single AirDensity => ContainsKey("AirDensity") ? (Single)this["AirDensity"] : 0;

        /// <summary>
        /// Pressure of air at start/finish line
        /// </summary>
        public Single AirPressure => ContainsKey("AirPressure") ? (Single)this["AirPressure"] : 0;

        /// <summary>
        /// Wind velocity at start/finish line
        /// </summary>
        public Single WindVel => ContainsKey("WindVel") ? (Single)this["WindVel"] : 0;

        /// <summary>
        /// Wind direction at start/finish line
        /// </summary>
        public Single WindDir => ContainsKey("WindDir") ? (Single)this["WindDir"] : 0;

        /// <summary>
        /// Relative Humidity
        /// </summary>
        public Single RelativeHumidity => ContainsKey("RelativeHumidity") ? (Single)this["RelativeHumidity"] : 0;

        /// <summary>
        /// Fog level
        /// </summary>
        public Single FogLevel => ContainsKey("FogLevel") ? (Single)this["FogLevel"] : 0;

        /// <summary>
        /// Status of driver change lap requirements
        /// </summary>
        public Int32 DCLapStatus => ContainsKey("DCLapStatus") ? (Int32)this["DCLapStatus"] : 0;

        /// <summary>
        /// Number of team drivers who have run a stint
        /// </summary>
        public Int32 DCDriversSoFar => ContainsKey("DCDriversSoFar") ? (Int32)this["DCDriversSoFar"] : 0;

        /// <summary>
        /// True if it is ok to reload car textures at this time
        /// </summary>
        public Boolean OkToReloadTextures => ContainsKey("OkToReloadTextures") && (Boolean)this["OkToReloadTextures"];

        /// <summary>
        /// Time left for mandatory pit repairs if repairs are active
        /// </summary>
        public Single PitRepairLeft => ContainsKey("PitRepairLeft") ? (Single)this["PitRepairLeft"] : 0;

        /// <summary>
        /// Time left for optional repairs if repairs are active
        /// </summary>
        public Single PitOptRepairLeft => ContainsKey("PitOptRepairLeft") ? (Single)this["PitOptRepairLeft"] : 0;

        /// <summary>
        /// Active camera's focus car index
        /// </summary>
        public Int32 CamCarIdx => ContainsKey("CamCarIdx") ? (Int32)this["CamCarIdx"] : 0;

        /// <summary>
        /// Active camera number
        /// </summary>
        public Int32 CamCameraNumber => ContainsKey("CamCameraNumber") ? (Int32)this["CamCameraNumber"] : 0;

        /// <summary>
        /// Active camera group number
        /// </summary>
        public Int32 CamGroupNumber => ContainsKey("CamGroupNumber") ? (Int32)this["CamGroupNumber"] : 0;

        /// <summary>
        /// State of camera system
        /// </summary>
        public Int32 CamCameraState => ContainsKey("CamCameraState") ? (Int32)this["CamCameraState"] : 0;

        /// <summary>
        /// 1=Car on track physics running
        /// </summary>
        public Boolean IsOnTrackCar => ContainsKey("IsOnTrackCar") && (Boolean)this["IsOnTrackCar"];

        /// <summary>
        /// 1=Car in garage physics running
        /// </summary>
        public Boolean IsInGarage => ContainsKey("IsInGarage") && (Boolean)this["IsInGarage"];

        /// <summary>
        /// Output torque on steering shaft
        /// </summary>
        public Single SteeringWheelTorque => ContainsKey("SteeringWheelTorque") ? (Single)this["SteeringWheelTorque"] : 0;

        /// <summary>
        /// Force feedback % max torque on steering shaft unsigned
        /// </summary>
        public Single SteeringWheelPctTorque => ContainsKey("SteeringWheelPctTorque") ? (Single)this["SteeringWheelPctTorque"] : 0;

        /// <summary>
        /// Force feedback % max torque on steering shaft signed
        /// </summary>
        public Single SteeringWheelPctTorqueSign => ContainsKey("SteeringWheelPctTorqueSign") ? (Single)this["SteeringWheelPctTorqueSign"] : 0;

        /// <summary>
        /// Force feedback % max torque on steering shaft signed stops
        /// </summary>
        public Single SteeringWheelPctTorqueSignStops => ContainsKey("SteeringWheelPctTorqueSignStops") ? (Single)this["SteeringWheelPctTorqueSignStops"] : 0;

        /// <summary>
        /// Force feedback % max damping
        /// </summary>
        public Single SteeringWheelPctDamper => ContainsKey("SteeringWheelPctDamper") ? (Single)this["SteeringWheelPctDamper"] : 0;

        /// <summary>
        /// Steering wheel max angle
        /// </summary>
        public Single SteeringWheelAngleMax => ContainsKey("SteeringWheelAngleMax") ? (Single)this["SteeringWheelAngleMax"] : 0;

        /// <summary>
        /// DEPRECATED use DriverCarSLBlinkRPM instead
        /// </summary>
        public Single ShiftIndicatorPct => ContainsKey("ShiftIndicatorPct") ? (Single)this["ShiftIndicatorPct"] : 0;

        /// <summary>
        /// Friction torque applied to gears when shifting or grinding
        /// </summary>
        public Single ShiftPowerPct => ContainsKey("ShiftPowerPct") ? (Single)this["ShiftPowerPct"] : 0;

        /// <summary>
        /// RPM of shifter grinding noise
        /// </summary>
        public Single ShiftGrindRPM => ContainsKey("ShiftGrindRPM") ? (Single)this["ShiftGrindRPM"] : 0;

        /// <summary>
        /// Raw throttle input 0=off throttle to 1=full throttle
        /// </summary>
        public Single ThrottleRaw => ContainsKey("ThrottleRaw") ? (Single)this["ThrottleRaw"] : 0;

        /// <summary>
        /// Raw brake input 0=brake released to 1=max pedal force
        /// </summary>
        public Single BrakeRaw => ContainsKey("BrakeRaw") ? (Single)this["BrakeRaw"] : 0;

        /// <summary>
        /// Peak torque mapping to direct input units for FFB
        /// </summary>
        public Single SteeringWheelPeakForceNm => ContainsKey("SteeringWheelPeakForceNm") ? (Single)this["SteeringWheelPeakForceNm"] : 0;

        /// <summary>
        /// Bitfield for warning lights
        /// </summary>
        public EngineWarnings EngineWarnings => ContainsKey("EngineWarnings") ? (EngineWarnings)(Int32)this["EngineWarnings"] : EngineWarnings.None;

        /// <summary>
        /// Liters of fuel remaining
        /// </summary>
        public Single FuelLevel => ContainsKey("FuelLevel") ? (Single)this["FuelLevel"] : 0;

        /// <summary>
        /// Percent fuel remaining
        /// </summary>
        public Single FuelLevelPct => ContainsKey("FuelLevelPct") ? (Single)this["FuelLevelPct"] : 0;

        /// <summary>
        /// Bitfield of pit service checkboxes
        /// </summary>
        public Int32 PitSvFlags => ContainsKey("PitSvFlags") ? (Int32)this["PitSvFlags"] : 0;

        /// <summary>
        /// Pit service left front tire pressure
        /// </summary>
        public Single PitSvLFP => ContainsKey("PitSvLFP") ? (Single)this["PitSvLFP"] : 0;

        /// <summary>
        /// Pit service right front tire pressure
        /// </summary>
        public Single PitSvRFP => ContainsKey("PitSvRFP") ? (Single)this["PitSvRFP"] : 0;

        /// <summary>
        /// Pit service left rear tire pressure
        /// </summary>
        public Single PitSvLRP => ContainsKey("PitSvLRP") ? (Single)this["PitSvLRP"] : 0;

        /// <summary>
        /// Pit service right rear tire pressure
        /// </summary>
        public Single PitSvRRP => ContainsKey("PitSvRRP") ? (Single)this["PitSvRRP"] : 0;

        /// <summary>
        /// Pit service fuel add amount
        /// </summary>
        public Single PitSvFuel => ContainsKey("PitSvFuel") ? (Single)this["PitSvFuel"] : 0;

        /// <summary>
        /// Replay playback speed
        /// </summary>
        public Int32 ReplayPlaySpeed => ContainsKey("ReplayPlaySpeed") ? (Int32)this["ReplayPlaySpeed"] : 0;

        /// <summary>
        /// 0=not slow motion  1=replay is in slow motion
        /// </summary>
        public Boolean ReplayPlaySlowMotion => ContainsKey("ReplayPlaySlowMotion") && (Boolean)this["ReplayPlaySlowMotion"];

        /// <summary>
        /// Seconds since replay session start
        /// </summary>
        public Double ReplaySessionTime => ContainsKey("ReplaySessionTime") ? (Double)this["ReplaySessionTime"] : 0;

        /// <summary>
        /// Replay session number
        /// </summary>
        public Int32 ReplaySessionNum => ContainsKey("ReplaySessionNum") ? (Int32)this["ReplaySessionNum"] : 0;

        /// <summary>
        /// In car front anti roll bar adjustment
        /// </summary>
        public Single dcAntiRollFront => ContainsKey("dcAntiRollFront") ? (Single)this["dcAntiRollFront"] : 0;

        /// <summary>
        /// In car brake bias adjustment
        /// </summary>
        public Single dcBrakeBias => ContainsKey("dcBrakeBias") ? (Single)this["dcBrakeBias"] : 0;

        /// <summary>
        /// In car traction control adjustment
        /// </summary>
        public Single dcTractionControl => ContainsKey("dcTractionControl") ? (Single)this["dcTractionControl"] : 0;

        /// <summary>
        /// In car abs adjustment
        /// </summary>
        public Single dcABS => ContainsKey("dcABS") ? (Single)this["dcABS"] : 0;

        /// <summary>
        /// In car throttle shape adjustment
        /// </summary>
        public Single dcThrottleShape => ContainsKey("dcThrottleShape") ? (Single)this["dcThrottleShape"] : 0;

        /// <summary>
        /// In car fuel mixture adjustment
        /// </summary>
        public Single dcFuelMixture => ContainsKey("dcFuelMixture") ? (Single)this["dcFuelMixture"] : 0;

        /// <summary>
        /// Pitstop qtape adjustment
        /// </summary>
        public Single dpQtape => ContainsKey("dpQtape") ? (Single)this["dpQtape"] : 0;

        /// <summary>
        /// Pitstop wedge adjustment
        /// </summary>
        public Single dpWedgeAdj => ContainsKey("dpWedgeAdj") ? (Single)this["dpWedgeAdj"] : 0;

        /// <summary>
        /// In car rear anti roll bar adjustment
        /// </summary>
        public Single dcAntiRollRear => ContainsKey("dcAntiRollRear") ? (Single)this["dcAntiRollRear"] : 0;

        /// <summary>
        /// Pitstop rear wing adjustment
        /// </summary>
        public Single dpRWingSetting => ContainsKey("dpRWingSetting") ? (Single)this["dpRWingSetting"] : 0;

        /// <summary>
        /// Engine coolant temp
        /// </summary>
        public Single WaterTemp => ContainsKey("WaterTemp") ? (Single)this["WaterTemp"] : 0;

        /// <summary>
        /// Engine coolant level
        /// </summary>
        public Single WaterLevel => ContainsKey("WaterLevel") ? (Single)this["WaterLevel"] : 0;

        /// <summary>
        /// Engine fuel pressure
        /// </summary>
        public Single FuelPress => ContainsKey("FuelPress") ? (Single)this["FuelPress"] : 0;

        /// <summary>
        /// Engine fuel used instantaneous
        /// </summary>
        public Single FuelUsePerHour => ContainsKey("FuelUsePerHour") ? (Single)this["FuelUsePerHour"] : 0;

        /// <summary>
        /// Engine oil temperature
        /// </summary>
        public Single OilTemp => ContainsKey("OilTemp") ? (Single)this["OilTemp"] : 0;

        /// <summary>
        /// Engine oil pressure
        /// </summary>
        public Single OilPress => ContainsKey("OilPress") ? (Single)this["OilPress"] : 0;

        /// <summary>
        /// Engine oil level
        /// </summary>
        public Single OilLevel => ContainsKey("OilLevel") ? (Single)this["OilLevel"] : 0;

        /// <summary>
        /// Engine voltage
        /// </summary>
        public Single Voltage => ContainsKey("Voltage") ? (Single)this["Voltage"] : 0;

        /// <summary>
        /// Engine manifold pressure
        /// </summary>
        public Single ManifoldPress => ContainsKey("ManifoldPress") ? (Single)this["ManifoldPress"] : 0;

        /// <summary>
        /// RR brake line pressure
        /// </summary>
        public Single RRbrakeLinePress => ContainsKey("RRbrakeLinePress") ? (Single)this["RRbrakeLinePress"] : 0;

        /// <summary>
        /// RR tire cold pressure  as set in the garage
        /// </summary>
        public Single RRcoldPressure => ContainsKey("RRcoldPressure") ? (Single)this["RRcoldPressure"] : 0;

        /// <summary>
        /// RR tire left carcass temperature
        /// </summary>
        public Single RRtempCL => ContainsKey("RRtempCL") ? (Single)this["RRtempCL"] : 0;

        /// <summary>
        /// RR tire middle carcass temperature
        /// </summary>
        public Single RRtempCM => ContainsKey("RRtempCM") ? (Single)this["RRtempCM"] : 0;

        /// <summary>
        /// RR tire right carcass temperature
        /// </summary>
        public Single RRtempCR => ContainsKey("RRtempCR") ? (Single)this["RRtempCR"] : 0;

        /// <summary>
        /// RR tire left percent tread remaining
        /// </summary>
        public Single RRwearL => ContainsKey("RRwearL") ? (Single)this["RRwearL"] : 0;

        /// <summary>
        /// RR tire middle percent tread remaining
        /// </summary>
        public Single RRwearM => ContainsKey("RRwearM") ? (Single)this["RRwearM"] : 0;

        /// <summary>
        /// RR tire right percent tread remaining
        /// </summary>
        public Single RRwearR => ContainsKey("RRwearR") ? (Single)this["RRwearR"] : 0;

        /// <summary>
        /// LR brake line pressure
        /// </summary>
        public Single LRbrakeLinePress => ContainsKey("LRbrakeLinePress") ? (Single)this["LRbrakeLinePress"] : 0;

        /// <summary>
        /// LR tire cold pressure  as set in the garage
        /// </summary>
        public Single LRcoldPressure => ContainsKey("LRcoldPressure") ? (Single)this["LRcoldPressure"] : 0;

        /// <summary>
        /// LR tire left carcass temperature
        /// </summary>
        public Single LRtempCL => ContainsKey("LRtempCL") ? (Single)this["LRtempCL"] : 0;

        /// <summary>
        /// LR tire middle carcass temperature
        /// </summary>
        public Single LRtempCM => ContainsKey("LRtempCM") ? (Single)this["LRtempCM"] : 0;

        /// <summary>
        /// LR tire right carcass temperature
        /// </summary>
        public Single LRtempCR => ContainsKey("LRtempCR") ? (Single)this["LRtempCR"] : 0;

        /// <summary>
        /// LR tire left percent tread remaining
        /// </summary>
        public Single LRwearL => ContainsKey("LRwearL") ? (Single)this["LRwearL"] : 0;

        /// <summary>
        /// LR tire middle percent tread remaining
        /// </summary>
        public Single LRwearM => ContainsKey("LRwearM") ? (Single)this["LRwearM"] : 0;

        /// <summary>
        /// LR tire right percent tread remaining
        /// </summary>
        public Single LRwearR => ContainsKey("LRwearR") ? (Single)this["LRwearR"] : 0;

        /// <summary>
        /// RF brake line pressure
        /// </summary>
        public Single RFbrakeLinePress => ContainsKey("RFbrakeLinePress") ? (Single)this["RFbrakeLinePress"] : 0;

        /// <summary>
        /// RF tire cold pressure  as set in the garage
        /// </summary>
        public Single RFcoldPressure => ContainsKey("RFcoldPressure") ? (Single)this["RFcoldPressure"] : 0;

        /// <summary>
        /// RF tire left carcass temperature
        /// </summary>
        public Single RFtempCL => ContainsKey("RFtempCL") ? (Single)this["RFtempCL"] : 0;

        /// <summary>
        /// RF tire middle carcass temperature
        /// </summary>
        public Single RFtempCM => ContainsKey("RFtempCM") ? (Single)this["RFtempCM"] : 0;

        /// <summary>
        /// RF tire right carcass temperature
        /// </summary>
        public Single RFtempCR => ContainsKey("RFtempCR") ? (Single)this["RFtempCR"] : 0;

        /// <summary>
        /// RF tire left percent tread remaining
        /// </summary>
        public Single RFwearL => ContainsKey("RFwearL") ? (Single)this["RFwearL"] : 0;

        /// <summary>
        /// RF tire middle percent tread remaining
        /// </summary>
        public Single RFwearM => ContainsKey("RFwearM") ? (Single)this["RFwearM"] : 0;

        /// <summary>
        /// RF tire right percent tread remaining
        /// </summary>
        public Single RFwearR => ContainsKey("RFwearR") ? (Single)this["RFwearR"] : 0;

        /// <summary>
        /// LF brake line pressure
        /// </summary>
        public Single LFbrakeLinePress => ContainsKey("LFbrakeLinePress") ? (Single)this["LFbrakeLinePress"] : 0;

        /// <summary>
        /// LF tire cold pressure  as set in the garage
        /// </summary>
        public Single LFcoldPressure => ContainsKey("LFcoldPressure") ? (Single)this["LFcoldPressure"] : 0;

        /// <summary>
        /// LF tire left carcass temperature
        /// </summary>
        public Single LFtempCL => ContainsKey("LFtempCL") ? (Single)this["LFtempCL"] : 0;

        /// <summary>
        /// LF tire middle carcass temperature
        /// </summary>
        public Single LFtempCM => ContainsKey("LFtempCM") ? (Single)this["LFtempCM"] : 0;

        /// <summary>
        /// LF tire right carcass temperature
        /// </summary>
        public Single LFtempCR => ContainsKey("LFtempCR") ? (Single)this["LFtempCR"] : 0;

        /// <summary>
        /// LF tire left percent tread remaining
        /// </summary>
        public Single LFwearL => ContainsKey("LFwearL") ? (Single)this["LFwearL"] : 0;

        /// <summary>
        /// LF tire middle percent tread remaining
        /// </summary>
        public Single LFwearM => ContainsKey("LFwearM") ? (Single)this["LFwearM"] : 0;

        /// <summary>
        /// LF tire right percent tread remaining
        /// </summary>
        public Single LFwearR => ContainsKey("LFwearR") ? (Single)this["LFwearR"] : 0;

        /// <summary>
        /// RR shock deflection
        /// </summary>
        public Single RRshockDefl => ContainsKey("RRshockDefl") ? (Single)this["RRshockDefl"] : 0;

        /// <summary>
        /// RR shock velocity
        /// </summary>
        public Single RRshockVel => ContainsKey("RRshockVel") ? (Single)this["RRshockVel"] : 0;

        /// <summary>
        /// LR shock deflection
        /// </summary>
        public Single LRshockDefl => ContainsKey("LRshockDefl") ? (Single)this["LRshockDefl"] : 0;

        /// <summary>
        /// LR shock velocity
        /// </summary>
        public Single LRshockVel => ContainsKey("LRshockVel") ? (Single)this["LRshockVel"] : 0;

        /// <summary>
        /// RF shock deflection
        /// </summary>
        public Single RFshockDefl => ContainsKey("RFshockDefl") ? (Single)this["RFshockDefl"] : 0;

        /// <summary>
        /// RF shock velocity
        /// </summary>
        public Single RFshockVel => ContainsKey("RFshockVel") ? (Single)this["RFshockVel"] : 0;

        /// <summary>
        /// LF shock deflection
        /// </summary>
        public Single LFshockDefl => ContainsKey("LFshockDefl") ? (Single)this["LFshockDefl"] : 0;

        /// <summary>
        /// LF shock velocity
        /// </summary>
        public Single LFshockVel => ContainsKey("LFshockVel") ? (Single)this["LFshockVel"] : 0;

        /// <summary>
        /// CR shock deflection
        /// </summary>
        public Single CRshockDefl => ContainsKey("CRshockDefl") ? (Single)this["CRshockDefl"] : 0;

        /// <summary>
        /// CR shock velocity
        /// </summary>
        public Single CRshockVel => ContainsKey("CRshockVel") ? (Single)this["CRshockVel"] : 0;

        /// <summary>
        /// CF shock deflection
        /// </summary>
        public Single CFshockDefl => ContainsKey("CFshockDefl") ? (Single)this["CFshockDefl"] : 0;

        /// <summary>
        /// CF shock velocity
        /// </summary>
        public Single CFshockVel => ContainsKey("CFshockVel") ? (Single)this["CFshockVel"] : 0;

	    /// <summary>
        /// RRSH shock deflection
        /// </summary>
        public Single RRSHshockDefl => ContainsKey("RRSHshockDefl") ? (Single)this["RRSHshockDefl"] : 0;

        /// <summary>
        /// LRSH shock deflection
        /// </summary>
        public Single LRSHshockDefl => ContainsKey("LRSHshockDefl") ? (Single)this["LRSHshockDefl"] : 0;

        /// <summary>
        /// RFSH shock deflection
        /// </summary>
        public Single RFSHshockDefl => ContainsKey("RFSHshockDefl") ? (Single)this["RFSHshockDefl"] : 0;

        /// <summary>
        /// LFSH shock deflection
        /// </summary>
        public Single LFSHshockDefl => ContainsKey("LFSHshockDefl") ? (Single)this["LFSHshockDefl"] : 0;

        /// <summary>
        /// 
        /// </summary>
        public Int32 TickCount => ContainsKey("TickCount") ? (Int32)this["TickCount"] : 0;
    }
}
