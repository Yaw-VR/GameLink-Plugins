using Newtonsoft.Json;

namespace YawVR_Game_Engine.Plugin
{
    //[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public struct XwaPlayer
    {
        [JsonProperty("XWA.player.shipname")]
        public string? ShipName;

        [JsonProperty("XWA.player.crafttypename")]
        public string? CraftTypeName;

        [JsonProperty("XWA.player.shortcrafttypename")]
        public string? ShortCraftTypeName;

        [JsonProperty("XWA.player.speed")]
        public int Speed;

        [JsonProperty("XWA.player.throttle")]
        public int Throttle;

        [JsonProperty("XWA.player.ELSlasers")]
        public int ELSlasers;

        [JsonProperty("XWA.player.ELSshields")]
        public int ELSshields;

        [JsonProperty("XWA.player.ELSbeam")]
        public int ELSbeam;

        [JsonProperty("XWA.player.s-foils")]
        public int SFoils;

        [JsonProperty("XWA.player.shielddirection")]
        public int ShieldDirection;

        [JsonProperty("XWA.player.shieldfront")]
        public int ShieldFront;

        [JsonProperty("XWA.player.shieldback")]
        public int ShieldBack;

        [JsonProperty("XWA.player.hull")]
        public int Hull;

        [JsonProperty("XWA.player.shake")]
        public int Shake;

        [JsonProperty("XWA.player.beamactive")]
        public bool BeamActive;

        [JsonProperty("XWA.player.undertractorbeam")]
        public bool UnderTractorBeam;

        [JsonProperty("XWA.player.underjammingbeam")]
        public bool UnderJammingBeam;

        [JsonProperty("XWA.player.activeweapon")]
        public string? ActiveWeapon;

        [JsonProperty("XWA.player.laserfired")]
        public bool LaserFired;

        [JsonProperty("XWA.player.warheadfired")]
        public bool WarheadFired;

        [JsonProperty("XWA.player.yaw_inertia")]
        public double YawInertia;

        [JsonProperty("XWA.player.pitch_inertia")]
        public double PitchInertia;

        [JsonProperty("XWA.player.roll_inertia")]
        public double RollInertia;

        [JsonProperty("XWA.player.abs_yaw")]
        public double Yaw;

        [JsonProperty("XWA.player.abs_pitch")]
        public double Pitch;

        [JsonProperty("XWA.player.abs_roll")]
        public double Roll;

        [JsonProperty("XWA.player.accel_inertia")]
        public double AccelInertia;

        [JsonProperty("XWA.status.location")]
        public string Location;

        [JsonProperty("XWA.status.hangar")]
        public bool Hangar;
    }

    public enum HyperspaceState
    {
        space = 0,
        hyoerentry = 2,
        hyperexit = 3,
        hyperspace = 4,
        
    }

    internal enum HyperspacePhaseEnum
    {
        HS_INIT_ST = 0,             // Initial state, we're not even in Hyperspace
        HS_HYPER_ENTER_ST = 1,      // We're entering hyperspace
        HS_HYPER_TUNNEL_ST = 2,     // Traveling through the blue Hyperspace tunnel
        HS_HYPER_EXIT_ST = 3,       // HyperExit streaks are being rendered
    };

    public struct Telemetry
    {
        public float Speed;
        public float Throttle;
        public float ELSlasers;
        public float ELSshields;
        public float ELSbeam;
        public float SFoils;
        public float ShieldDirection;
        public float ShieldFront;
        public float ShieldBack;
        public float Hull;
        public float Shake;

        public float BeamActive;
        public float UnderTractorBeam;
        public float UnderJammingBeam;
        public float LaserFired;
        public float WarheadFired;

        public float YawInertia;
        public float PitchInertia;
        public float RollInertia;
        public float AccelInertia;

        public int HyperspacePhase;

        public int Hangar;

        public float Yaw; 
        public float Pitch; 
        public float Roll;

        

        

        

        public static explicit operator Telemetry(XwaPlayer src)
        {
            var retval = new Telemetry() 
            {
                Speed = src.Speed,
                Throttle = src.Throttle,
                ELSlasers = src.ELSlasers,
                ELSshields = src.ELSshields,
                ELSbeam = src.ELSbeam,
                SFoils = src.SFoils,
                ShieldDirection = src.ShieldDirection,
                ShieldFront = src.ShieldFront,
                ShieldBack = src.ShieldBack,
                Hull = src.Hull,
                Shake = src.Shake,
                BeamActive = src.BeamActive ? 1f : 0f,
                UnderTractorBeam = src.UnderTractorBeam ? 1f : 0f,
                UnderJammingBeam = src.UnderJammingBeam ? 1f : 0f,
                LaserFired = src.LaserFired ? 1f : 0f,
                WarheadFired = src.WarheadFired ? 1f : 0f,
                Yaw = (float)src.Yaw,
                Pitch = (float)src.Pitch,
                Roll = (float)src.Roll,
                YawInertia = (float)src.YawInertia / 100f,
                PitchInertia = (float)src.PitchInertia / 100f,
                RollInertia = (float)src.RollInertia,
                AccelInertia = (float)src.AccelInertia / 100f,
                Hangar = src.Hangar ? 1 : 0,
                HyperspacePhase = src.Location switch
                {
                    "space" => (int)HyperspaceState.space,
                    "hyperentry" => (int)HyperspaceState.hyoerentry,
                    "hyperexit" => (int)HyperspaceState.hyperexit,
                    "hyperspace" => (int)HyperspaceState.hyperspace,
                    _ => 0,
                },
            };

            

            return retval;
        }
    }
}