using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GT7Plugin
{
    internal struct GT7Output
    {
        public float Yaw;
        public float Pitch;
        public float Roll;
        public float Sway;
        public float Surge;
        public float Heave;
        public float Kph;
        public float MaxKph;
        public float RPM;
        public float TireFL_SusHeight;
        public float TireFR_SusHeight;
        public float TireRL_SusHeight;
        public float TireRR_SusHeight;
        public float OnTrack;
        public float IsPaused;
        public float Loading;
        public float InRace;
        public float CentripetalAcceleration;
    }
}
