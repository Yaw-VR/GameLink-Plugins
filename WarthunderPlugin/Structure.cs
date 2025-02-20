using System;

namespace WarthunderPlugin
{
    [Serializable]
    internal class Structure
    {
    public bool valid;
        public string type;
        public float speed, pedals1, pedals2, pedals3, stick_elevator, stick_ailerons, vario, altitude_hour, altitude_min, altitude_10k;

        public float aviahorizon_roll;
        public float aviahorizon_pitch;


        public float bank, turn, compass, compass1, clock_hour, clock_min, clock_sec, manifold_pressure, rpm, oil_pressure, oil_temperature, water_temperature, mixture, fuel1, gears, gears_lamp, flaps, trimmer, throttle, weapon1, prop_pitch;


    }
}
