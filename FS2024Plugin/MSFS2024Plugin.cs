using Microsoft.FlightSimulator.SimConnect;
using FS2024Plugin.Properties;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using YawGLAPI;
namespace MSFS2024
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Microsoft Flight Simulator 2024")]
    [ExportMetadata("Version", "1.0")]
    class FS2024Plugin : Game
    {
        private bool isSimRunning = false; // False is the user in menu, true if the user is in controll
        private bool isSimRunningPrevious = false;
        private float[] currentValues; // Stores the current values being sent
        private float[] targetValues;  // Stores the target values to transition to
        private bool isTransitioning = false; // Indicates if a transition is active
        private float transitionDuration = 1.5f; // Transition duration in seconds
        private float[] initialValues; // Values at the start of the transition
        private DateTime transitionStartTime;

        private float yaw_immersive = 0.0f;
        private float yaw_immersive_twist = 0.0f;
        private float previous_yaw = 0.0f;
        private float previous_roll_immersive_temp = 0.0f;

        // SimConnect object
        enum DEFINITIONS
        {
            Struct1
        }

        enum DATA_REQUESTS
        {
            REQUEST_1,
        };

        private enum SimEvent
        {
            SIM_START,
            SIM_STOP,
            PAUSE_EX1
        }

        // Pause state flags
        private const int PAUSE_STATE_FLAG_OFF = 0;
        private const int PAUSE_STATE_FLAG_PAUSE = 1;
        private const int PAUSE_STATE_FLAG_ACTIVE_PAUSE = 4;
        private const int PAUSE_STATE_FLAG_SIM_PAUSE = 8;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct Struct1
        {
            // this is how you declare a fixed size string
            public double latitude;
            public double longitude;
            public double altitude;
            public double pitch;
            public double roll;
            public double heading;
            public double vel_x;
            public double vel_y;
            public double vel_z;
            public double velocity;
            public double engine_rpm_1;
            public double engine_rpm_2;
            public double engine_rpm_3;
            public double engine_rpm_4;
            public double relavite_wind_acc_body_x;
            public double relavite_wind_acc_body_y;
            public double relavite_wind_acc_body_z;
            public double rotation_acc_body_x;
            public double rotation_acc_body_y;
            public double rotation_acc_body_z;
            public double aircraft_wind_x;
            public double aircraft_wind_y;
            public double aircraft_wind_z;
            public double crenter_wheel_rpm;
            public double gear_hydraulic_pressure;
            public double turn_coordinator_ball;
            public double elevator_position;
            public double aileron_position;

            public double ContactPointCompression0;
            public double ContactPointCompression1;
            public double ContactPointCompression2;
            public double ContactPointCompression3;
            public double ContactPointCompression4;
            public double ContactPointCompression5;
            public int contactPointIsOnGround0;
            public int contactPointIsOnGround1;
            public int contactPointIsOnGround2;
            public int contactPointIsOnGround3;
            public int contactPointIsOnGround4;
            public int contactPointIsOnGround5;
            public double engineVibration1;
            public double engineVibration2;
            public double engineVibration3;
            public double engineVibration4;

            public double gForce;
            public double accelerationBodyX;
            public double accelerationBodyY;
            public double accelerationBodyZ;
            public double planeBankDegrees;
            public double planeHeadingDegreesGyro;
            public double planeHeadingDegreesMagnetic;
            public double planeHeadingDegreesTrue;
            public double planePitchDegrees;
            public double rotationAccelerationBodyX;
            public double rotationAccelerationBodyY;
            public double rotationAccelerationBodyZ;
            public double rotationVelocityBodyX;
            public double rotationVelocityBodyY;
            public double rotationVelocityBodyZ;
            public double velocityBodyX;
            public double velocityBodyY;
            public double velocityBodyZ;
            public double verticalSpeed;
            public int stallWarning;
            public double variometerRate;
            public double engRotorRpm1;
            public double engRotorRpm2;
            public double engRotorRpm3;
            public double engRotorRpm4;
        };

        // User-defined win32 event
        const int WM_USER_SIMCONNECT = 0x0402;
        public string PROCESS_NAME => "FlightSimulator2024";
        public int STEAM_ID => 2537590;
        public string AUTHOR => "YawVR";
        public bool PATCH_AVAILABLE => true;

        public string Description => Resources.description;
        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        private Thread readThread;

        //WIND GENERATOR
        float t = 0;
        float periodScale = 1f;
        Random r = new Random();
        private IMainFormDispatcher dispatcher;
        private IProfileManager controller;
        private volatile bool stop = false;

        public IntPtr Handle { get; private set; }

        public List<Profile_Component> DefaultProfile()
        {
            return dispatcher.JsonToComponents(Resources.defProfile);
        }
        public LedEffect DefaultLED()
        {

            return dispatcher.JsonToLED(Resources.defProfile);
        }

        public void Exit()
        {
            stop = true;

        }

        public string[] GetInputData()
        {
            Type f = typeof(Struct1);
            FieldInfo[] fields = f.GetFields();

            string[] inputs = new string[fields.Length + 4];  // + pitch_multiplier, roll_immersive, yaw_immersive, yaw_immersive_twist

            for (int i = 0; i < fields.Length; i++)
            {
                inputs[i] = fields[i].Name;
            }
            inputs[fields.Length] = "pitch_multiplier";
            inputs[fields.Length + 1] = "roll_immersive";
            inputs[fields.Length + 2] = "yaw_immersive";
            inputs[fields.Length + 3] = "yaw_immersive_twist";

            return inputs;


        }
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.controller = controller;
        }
        public void Init()
        {
            try
            {
                SimConnect c;
                stop = false;
                readThread = new Thread(new ThreadStart(ReadFunction));
                readThread.Start();
            }
            catch (Exception ex)
            {

                dispatcher.ShowNotification(NotificationType.ERROR, "Cannot load Simconnect. Make sure to click patch first!");
            }

        }

        private SimConnect ConnectToSimconnect()
        {
            try
            {
                Debug.WriteLine("Trying to conect to MSFS..");
                SimConnect simconnect = null;
                simconnect = new SimConnect("Managed Data Request", this.Handle, WM_USER_SIMCONNECT, null, 0);

                // define a data structure
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE LATITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE LONGITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE ALTITUDE", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.00f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.00f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE HEADING DEGREES TRUE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.00f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY X", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY Y", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY Z", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GROUND VELOCITY", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG RPM:1", "RPM", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG RPM:2", "RPM", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG RPM:3", "RPM", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG RPM:4", "RPM", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RELATIVE WIND VELOCITY BODY X", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RELATIVE WIND VELOCITY BODY Y", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RELATIVE WIND VELOCITY BODY Z", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION ACCELERATION BODY X", "degrees per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION ACCELERATION BODY Y", "degrees per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION ACCELERATION BODY Z", "degrees per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AIRCRAFT WIND X", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AIRCRAFT WIND Y", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AIRCRAFT WIND Z", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CENTER WHEEL RPM", "rpm", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GEAR HYDRAULIC PRESSURE", "Pound force per square foot", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "TURN COORDINATOR BALL", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ELEVATOR POSITION", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AILERON POSITION", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT COMPRESSION:0", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT COMPRESSION:1", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT COMPRESSION:2", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT COMPRESSION:3", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT COMPRESSION:4", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT COMPRESSION:5", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT IS ON GROUND:0", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT IS ON GROUND:1", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT IS ON GROUND:2", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT IS ON GROUND:3", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT IS ON GROUND:4", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CONTACT POINT IS ON GROUND:5", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG VIBRATION:1", "number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG VIBRATION:2", "number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG VIBRATION:3", "number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG VIBRATION:4", "number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "G FORCE", "G force", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ACCELERATION BODY X", "feet per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ACCELERATION BODY Y", "feet per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ACCELERATION BODY Z", "feet per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE HEADING DEGREES GYRO", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE HEADING DEGREES MAGNETIC", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE HEADING DEGREES TRUE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION ACCELERATION BODY X", "degrees per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION ACCELERATION BODY Y", "degrees per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION ACCELERATION BODY Z", "degrees per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY X", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY Y", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY Z", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "VELOCITY BODY X", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "VELOCITY BODY Y", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "VELOCITY BODY Z", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "VERTICAL SPEED", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "STALL WARNING", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "VARIOMETER RATE", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG ROTOR RPM:1", "percent scaler 16k", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG ROTOR RPM:2", "percent scaler 16k", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG ROTOR RPM:3", "percent scaler 16k", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG ROTOR RPM:4", "percent scaler 16k", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                // IMPORTANT: register it with the simconnect managed wrapper marshaller
                // if you skip this step, you will only receive a uint in the .dwData field.
                simconnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);

                // catch a simobject data request
                simconnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(simconnect_OnRecvSimobjectDataBytype);

                // Subscribe to SIM_START and SIM_END to determine if the 
                simconnect.SubscribeToSystemEvent(SimEvent.SIM_START, "SimStart");
                simconnect.SubscribeToSystemEvent(SimEvent.SIM_STOP, "SimStop");
                simconnect.SubscribeToSystemEvent(SimEvent.PAUSE_EX1, "Pause_EX1");
                // Hook up event handler
                simconnect.OnRecvEvent += SimConnect_OnRecvEvent;

                return simconnect;
            }
            catch (Exception ex)
            {
                return null;
                //dispatcher.DialogShow("Could not connect to MSFS", DIALOG_TYPE.INFO);
                //dispatcher.ExitGame();
            }

        }

        public void SimConnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            if ((SimEvent)data.uEventID == SimEvent.SIM_START)
            {
                isSimRunning = true;
                Debug.WriteLine("Simulation started.");
            }
            else if ((SimEvent)data.uEventID == SimEvent.SIM_STOP)
            {
                isSimRunning = false;
                Debug.WriteLine("Simulation stopped.");
            }
            else if ((SimEvent)data.uEventID == SimEvent.PAUSE_EX1)
            {
                HandlePauseState((int)data.dwData);
            }
        }

        // Handle detailed pause state
        private void HandlePauseState(int pauseState)
        {
            if (pauseState == PAUSE_STATE_FLAG_OFF)
            {
                isSimRunning = true; // No pause
                Debug.WriteLine("Simulation resumed.");
            }
            else if (pauseState == PAUSE_STATE_FLAG_PAUSE)
            {
                isSimRunning = false; // Full pause
                Debug.WriteLine("Simulation fully paused.");
            }
            else if (pauseState == PAUSE_STATE_FLAG_ACTIVE_PAUSE)
            {
                isSimRunning = false; // Active pause
                Debug.WriteLine("Simulation in active pause.");
            }
            else if (pauseState == PAUSE_STATE_FLAG_SIM_PAUSE)
            {
                isSimRunning = false; // Sim paused, but other elements still running
                Debug.WriteLine("Simulation paused, but traffic and multiplayer are running.");
            }
            else
            {
                Debug.WriteLine($"Unknown pause state: {pauseState}");
            }
        }

        private void simconnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.REQUEST_1:
                    // Retrieve the data as the specified structure
                    Struct1 s1 = (Struct1)data.dwData[0];
                    FieldInfo[] fields = typeof(Struct1).GetFields();

                    // Initialize arrays if null
                    if (currentValues == null)
                    {
                        currentValues = new float[fields.Length + 4]; // + pitch_multiplier, roll_immersive, yaw_immersive, yaw_immersive_twist
                        targetValues = new float[fields.Length + 4];
                    }

                    // Suppress telemtry during loading screens -> The altitude is higher than 53819 ín this false data!
                    isSimRunning = isSimRunning && s1.altitude < 53819f;

                    // Fix tipping point problem with the raw roll and yaw values

                    float pitch = (float)s1.pitch;
                    float yaw = (float)s1.heading;
                    float roll = (float)s1.roll;
                    float pitch_multiplier = s1.altitude < 53819f ? (float)Math.Cos(Convert.ToDouble(pitch) * Math.PI / 180.0) : 0.0f;

                    float roll_immersive_temp;
                    if (roll < -90)
                    {
                        roll_immersive_temp = -180 - roll;
                    }
                    else if (roll > 90)
                    {
                        roll_immersive_temp = 180 - roll;
                    }
                    else
                    {
                        roll_immersive_temp = roll;
                    }

                    float roll_immersive = pitch_multiplier * roll_immersive_temp;

                    yaw_immersive = NormalizeAngle(yaw_immersive - (pitch_multiplier * NormalizeAngle(previous_yaw - yaw)));
                    yaw_immersive_twist = NormalizeAngle(yaw_immersive - (pitch_multiplier * NormalizeAngle(previous_yaw - yaw) + (1 - pitch_multiplier) * NormalizeAngle(previous_roll_immersive_temp - roll_immersive_temp)));



                    if (!isSimRunning)
                    {

                        if (isSimRunningPrevious)
                        {
                            // Start transitioning to zero if paused
                            isTransitioning = true;
                            transitionStartTime = DateTime.Now;
                            initialValues = (float[])targetValues.Clone(); // target values from the step before
                        }

                        for (int i = 0; i < fields.Length; i++)
                        {
                            if(fields[i].Name == "heading") // Keep heading the same
                            {
                                targetValues[i] = (float)Math.Round(Convert.ToSingle(fields[i].GetValue(s1)), 3);
                            }
                            else
                            {
                                targetValues[i] = 0.0f;
                            }
                        }
                        targetValues[fields.Length] = 0.0f;
                        targetValues[fields.Length + 1] = 0.0f;
                        targetValues[fields.Length + 2] = yaw_immersive; // Keep the yaw the same
                        targetValues[fields.Length + 3] = yaw_immersive_twist;
                    }
                    else if (isSimRunning)
                    {
                        if (!isSimRunningPrevious)
                        {
                            // Start transitioning to actual data if resumed
                            isTransitioning = true;
                            transitionStartTime = DateTime.Now;
                            initialValues = (float[])targetValues.Clone();
                        }

                        for (int i = 0; i < fields.Length; i++)
                        {
                            float value = Convert.ToSingle(fields[i].GetValue(s1));
                            targetValues[i] = float.IsNaN(value) ? 0.0f : (float)Math.Round(value, 3);
                        }
                        targetValues[fields.Length] = pitch_multiplier;
                        targetValues[fields.Length + 1] = roll_immersive;
                        targetValues[fields.Length + 2] = yaw_immersive;
                        targetValues[fields.Length + 3] = yaw_immersive_twist;
                    }

                    isSimRunningPrevious = isSimRunning;


                    if (isTransitioning)
                    {
                        DateTime currentTime = DateTime.Now;
                        float elapsedTime = (float)(currentTime - transitionStartTime).TotalSeconds;

                        // Calculate interpolation factor (0.0 to 1.0)
                        float t = Math.Min(elapsedTime / transitionDuration, 1.0f);

                        // Flag for transition completion
                        bool transitionComplete = t >= 1.0f;

                        for (int i = 0; i < fields.Length + 4; i++)
                        {
                            // Linear interpolation between initialValues and targetValues
                            currentValues[i] = initialValues[i] + (targetValues[i] - initialValues[i]) * t;

                            // Set controller input
                            controller.SetInput(i, currentValues[i]);
                        }

                        if (transitionComplete)
                        {
                            isTransitioning = false; // End transition
                        }
                    }
                    else
                    {
                        // Update values directly without transitioning
                        for (int i = 0; i < fields.Length + 4; i++)
                        {
                            currentValues[i] = targetValues[i];
                            controller.SetInput(i, currentValues[i]);
                        }
                    }

                    previous_yaw = yaw;
                    previous_roll_immersive_temp = roll_immersive_temp;


                    break;

                default:
                    Debug.WriteLine("Unknown request ID: " + data.dwRequestID);
                    break;
            }
        }

        private float NormalizeAngle(float angle)
        {
            // Bring the angle within the range [-180, 180]
            while (angle > 180.0f) angle -= 360.0f;
            while (angle < -180.0f) angle += 360.0f;
            return angle;
        }

        private void ReadFunction()
        {
            SimConnect simconnect;
            do
            {
                Thread.Sleep(500);
                simconnect = ConnectToSimconnect();
            } while (simconnect == null && !stop);

            try
            {
                dispatcher.ShowNotification(NotificationType.INFO, "Connected to Simconnect");
                while (!stop)
                {
                    // The following call returns identical information to:
                    // simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_1, DEFINITIONS.Struct1, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE);

                    simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_1, DEFINITIONS.Struct1, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                    simconnect.ReceiveMessage();
                    Thread.Sleep(1);
                }

            }
            catch (COMException)
            {
                dispatcher.ExitGame();
            }



        }


        public void PatchGame()
        {
            try
            {
                string tempFile = Path.GetTempFileName() + ".dll";
                string tempFile2 = Path.GetTempFileName() + ".dll";

                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile("http://yaw.one/gameengine/Plugins/Microsoft_Flight_Simulator_XX/Microsoft.FlightSimulator.SimConnect.dll", tempFile);
                    wc.DownloadFile("http://yaw.one/gameengine/Plugins/Microsoft_Flight_Simulator_XX/SimConnect.dll", tempFile2);
                }

                var entryPointLocation = Assembly.GetEntryAssembly().Location;
                File.Copy(tempFile, Path.GetDirectoryName(entryPointLocation) + "/Gameplugins/Microsoft.FlightSimulator.SimConnect.dll", true);
                File.Copy(tempFile2, Path.GetDirectoryName(entryPointLocation) + "/SimConnect.dll", true);


                dispatcher.RestartApp(false);
            }
            catch (IOException e)
            {
                e.ToString();
                dispatcher.DialogShow("Already patched!", DIALOG_TYPE.INFO);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                dispatcher.DialogShow("No permission to install plugin. Restart with admin privileges?", DIALOG_TYPE.QUESTION, delegate {

                    dispatcher.RestartApp(false);
                });
            }
        }


        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return null;
        }
        Stream GetStream(string resourceName)
        {
            var assembly = GetType().Assembly;
            var rr = assembly.GetManifestResourceNames();
            string fullResourceName = $"{assembly.GetName().Name}.Resources.{resourceName}";
            return assembly.GetManifestResourceStream(fullResourceName);
        }
    }
}
