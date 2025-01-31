using System;
using System.Linq;

namespace TDUSCPlugin
{
    public class TDUSCPacket
    {
        /// <summary>
        /// 0.	Total Time(not reset after stage restart)
        /// </summary>
        float Time;           
        float LapTime;        //1	Current Lap/Stage Time(starts on Go!)
        float LapDistance;    //2	Current Lap/Stage Distance(meters)
        float TotalDistance;    //3	? (starts from 0) - if distance then not equal to above!

        
        public float PositionX;
        public float PositionY;
        public float PositionZ;

        /// <summary>
        /// 7. World Velocity(Speed) [m/s]
        /// </summary>
        public float Speed;

        
        public float VelocityX;
        public float VelocityY;
        public float VelocityZ;

        /// <summary>
        /// 11-13. World space right direction
        /// </summary>
        public float RollX;
        public float RollY;
        public float RollZ;

        /// <summary>
        /// 14-16. World space forward direction
        /// </summary>
        public float PitchX;
        public float PitchY;
        public float PitchZ;

        /// <summary>
        /// 17. Position of Suspension Rear Left
        /// </summary>
        public float Susp_pos_bl;

        /// <summary>
        /// 18. Position of Suspension Rear Right
        /// </summary>
        public float Susp_pos_br;

        /// <summary>
        /// 19. Position of Suspension Front Left
        /// </summary>
        public float Susp_pos_fl;

        /// <summary>
        /// 20. Position of Suspension Front Right
        /// </summary>
        public float Susp_pos_fr;    

        /// <summary>
        /// 21. Velocity of Suspension Rear Left
        /// </summary>
        public float Susp_vel_bl;

        /// <summary>
        /// 22. Velocity of Suspension Rear Right
        /// </summary>
        public float Susp_vel_br;
        
        /// <summary>
        /// 23.	Velocity of Suspension Front Left
        /// </summary>
        public float Susp_vel_fl;

        /// <summary>
        /// 24.Velocity of Suspension Front Right
        /// </summary>
        public float Susp_vel_fr;

        /// <summary>
        /// 25. Velocity of Wheel Rear Left
        /// </summary>
        public float Wheel_speed_bl;
        
        /// <summary>
        /// 26.	Velocity of Wheel Rear Right
        /// </summary>
        public float Wheel_speed_br; 
        
        /// <summary>
        /// 27.	Velocity of Wheel Front Left
        /// </summary>
        public float Wheel_speed_fl;

        /// <summary>
        /// 28.	Velocity of Wheel Front Right
        /// </summary>
        public float Wheel_speed_fr; 

        /// <summary>
        /// 29.	Position Throttle
        /// </summary>
        public float Throttle;   
        public float Steer;      //30	Position Steer
        public float Brake;      //31	Position Brake
        public float Clutch;     //32	Position Clutch
        public int Gear;         //33	Gear[0 = Neutral, 1 = 1, 2 = 2, ..., -1 = Reverse]

        /// <summary>
        /// 34.	G-Force Lateral
        /// </summary>
        public float Gforce_lat;

        /// <summary>
        /// 35.	G-Force Longitudinal
        /// </summary>
        public float Gforce_lon;

        /// <summary>
        /// 36. Current Lap(rx only)
        /// </summary>
        public float Lap;

        /// <summary>
        /// 37. Engine Speed[rpm / 10]
        /// </summary>
        public float RPM;


        ////public float Sli_pro_native_support;    // SLI Pro support	38	Not used
        public float Car_position;                  // car race position	39	Current Position (rx only)
        ////public float Kers_level;                // kers energy left	40	Not used
        ////public float Kers_max_level;            // kers maximum energy	41	Not used
        ////public float Drs;                       // 0 = off, 1 = on	42	Not used
        ////public float Traction_control;          // 0 (off) - 2 (high)	43	Not used
        ////public float Anti_lock_brakes;          // 0 (off) - 1 (on)	44	Not used
        ////public float Fuel_in_tank;              // current fuel mass	45	Not used
        ////public float Fuel_capacity;             // fuel capacity	46	Not used
        ////public float In_pits;                   // 0 = none, 1 = pitting, 2 = in pit area	47	Not used
        
        /// <summary>
        /// 
        /// </summary>
        public float Sector;                        // 0 = sector1, 1 = sector2; 2 = sector3	48	Sector
        public float Sector1_time;                  // time of sector1 (or 0)	49	Sector 1 time
        public float Sector2_time;                  // time of sector2 (or 0)	50	Sector 2 time

        /// <summary>
        /// brakes temperature (centigrade)	51	Temperature Brake in C
        /// </summary>        
        public float Brakes_temp;            // [4]; // brakes temperature (centigrade)	51	Temperature Brake in C

        public float Wheels_pressure;        //[4]; // wheels pressure PSI	52	Wheel pressure
        //public float TeaInfo;              // team ID	53	Not used
        public float Total_laps;             // total number of laps in this race	54	Total laps of the race (if SSS)
        //public float Track_size;             // track size meters	55	Not used
        public float Last_lap_time;          // last lap time	56	Last lap time (if SSS)
        public float Max_rpm;                // cars max RPM, at which point the rev limiter will kick in	57	Max rpm
        public float Idle_rpm;               // cars idle RPM	58	Idle rpm
        public float Max_gears;              // maximum number of gears	59	Number of gears
                                             //public float SessionType;            // 0 = unknown, 1 = practice, 2 = qualifying, 3 = race	60	Not used
                                             //public float DrsAllowed;             // 0 = not allowed, 1 = allowed, -1 = invalid / unknown	61	Not used
                                             //public float Track_number;           // -1 for unknown, 0-21 for tracks	62	Not used
                                             //public float VehicleFIAFlags;      // -1 = invalid/unknown, 0 = none, 1 = green, 2 = blue, 3 = yellow, 4 = red	63	Not used


        public TDUSCPacket(byte[] data)
        {
            Read(data);   
        }

        private void Read(byte[] data)
        {
            
            
            

            Time = BitConverter.ToSingle(data, 0);
            LapTime = BitConverter.ToSingle(data, 4);
            LapDistance = BitConverter.ToSingle(data, 8);
            TotalDistance = BitConverter.ToSingle(data, 12);

            PositionX = BitConverter.ToSingle(data, 16); 
            PositionY = BitConverter.ToSingle(data, 20);
            PositionZ = BitConverter.ToSingle(data, 24);

            Speed = BitConverter.ToSingle(data, 28);
            
            VelocityX = BitConverter.ToSingle(data, 32);
            VelocityY = BitConverter.ToSingle(data, 36);
            VelocityZ = BitConverter.ToSingle(data, 40);
 
            RollX = BitConverter.ToSingle(data, 44);
            RollY = BitConverter.ToSingle(data, 48);
            RollZ = BitConverter.ToSingle(data, 52);

            PitchX = BitConverter.ToSingle(data, 56);
            PitchY = BitConverter.ToSingle(data, 60);
            PitchZ = BitConverter.ToSingle(data, 64);

            Susp_pos_bl = BitConverter.ToSingle(data, 68);
            Susp_pos_br = BitConverter.ToSingle(data, 72);
            Susp_pos_fl = BitConverter.ToSingle(data, 76);
            Susp_pos_fr = BitConverter.ToSingle(data, 80);

            Susp_vel_bl = BitConverter.ToSingle(data, 84);
            Susp_vel_br = BitConverter.ToSingle(data, 88);
            Susp_vel_fl = BitConverter.ToSingle(data, 92);
            Susp_vel_fr = BitConverter.ToSingle(data, 96);

            Wheel_speed_bl = BitConverter.ToSingle(data, 100);
            Wheel_speed_br = BitConverter.ToSingle(data, 104);
            Wheel_speed_fl = BitConverter.ToSingle(data, 108);
            Wheel_speed_fr = BitConverter.ToSingle(data, 112);

            Throttle = BitConverter.ToSingle(data, 116);
            Steer = BitConverter.ToSingle(data, 120);
            Brake = BitConverter.ToSingle(data, 124);
            Clutch = BitConverter.ToSingle(data, 128);
            Gear = BitConverter.ToInt32(data, 132);

            Gforce_lat = BitConverter.ToSingle(data, 136);
            Gforce_lon = BitConverter.ToSingle(data, 140);
            Lap = BitConverter.ToSingle(data, 144);
            RPM = BitConverter.ToSingle(data, 148) / 30f;
            //Sli_pro_native_support = BitConverter.ToSingle(data, 152);
            Car_position = BitConverter.ToSingle(data, 156);
            //Kers_level = BitConverter.ToSingle(data, 160);
            //Kers_max_level = BitConverter.ToSingle(data, 164);
            //Drs = BitConverter.ToSingle(data, 168);
            //Traction_control = BitConverter.ToSingle(data, 172);
            //Anti_lock_brakes = BitConverter.ToSingle(data, 176);
            //Fuel_in_tank = BitConverter.ToSingle(data, 180);
            //Fuel_capacity = BitConverter.ToSingle(data, 184);
            //In_pits = BitConverter.ToSingle(data, 188);
            Sector = BitConverter.ToSingle(data, 192);
            Sector1_time = BitConverter.ToSingle(data, 196);
            Sector2_time = BitConverter.ToSingle(data, 200);
            Brakes_temp = BitConverter.ToSingle(data, 204);
            Wheels_pressure = BitConverter.ToSingle(data, 208);
            //TeaInfo = BitConverter.ToSingle(data, 212);
            Total_laps = BitConverter.ToSingle(data, 216);
            //Track_size = BitConverter.ToSingle(data, 220);
            Last_lap_time = BitConverter.ToSingle(data, 224);
            Max_rpm = BitConverter.ToSingle(data, 228);
            Idle_rpm = BitConverter.ToSingle(data, 232);
            Max_gears = BitConverter.ToSingle(data, 236);
            //SessionType = BitConverter.ToSingle(data, 240);
            //DrsAllowed = BitConverter.ToSingle(data, 244);
            //Track_number = BitConverter.ToSingle(data, 248);
            //VehicleFIAFlags = BitConverter.ToSingle(data, 252);
        }

        

        public bool IsZero(byte[] data)
        {
           return data.All(x => x == 0); 
        }
    }
}
