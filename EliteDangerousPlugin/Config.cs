using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YawGLAPI;

namespace EliteDangerousPlugin
{
    public struct Config
    {
        [Info(Description = "(velocity)")]
        public float MaxYawVelocity = 0.1f;

        [Info(Description = "(velocity)")]
        public float MaxPitchVelocity = 0.3f;

        [Info(Description = "(velocity)")]
        public float MaxRollVelocity = 0.6f;

        [Info(Description = "(move to zero speed)")]
        public float MoveToZeroSpeed = 0.02f;

        [Info(Description = "(ExponentialCurve)")]
        public float K = 9.0f;

        public Config()
        {
        }
    }
}
