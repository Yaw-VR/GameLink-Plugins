namespace YawGLAPI
{
    public interface IDeviceParameters
    {

        public byte Power { get; }
        public byte MaxRoll { get; }
        public byte MaxPitchFW { get; }
        public byte MaxPitchBW { get; }
        public byte RollLimit { get; }
        public byte PitchLimitF { get; }
        public byte PitchLimitB { get; }
        public uint YawLimit { get; }
        public float YawReturn { get; }
        public bool HasYawLimit { get; }
        public bool HasVibration {  get; }
        public byte VibrationLimit {  get; }
    }
}
