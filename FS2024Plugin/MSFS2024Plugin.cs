using Microsoft.FlightSimulator.SimConnect;
using FS2024Plugin.Properties;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using YawGLAPI;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;
using System.Reflection.Metadata;
using System.Drawing;
using System.Security.Cryptography;
namespace MSFS2024
{


    [Export(typeof(Game))]
    [ExportMetadata("Name", "Microsoft Flight Simulator 2024")]
    [ExportMetadata("Version", "2.1")]
    class FS2024Plugin : Game
    {
        private bool isSimRunning = false;          // False is the user in menu, true if the user is in control
        private bool isSimRunningPrevious = false;
        private int simPauseState;
        private float[] targetValues = null;        // Stores the target values to transition to
        private float[] initialValues = null;       // Values at the start of the transition
        private bool isTransitioning = false;       // Indicates if a transition is active
        private float transitionDuration = 1.5f;    // Transition duration in seconds
        private double transitionStartTime;
        private Stopwatch stopwatch = Stopwatch.StartNew();

        private double previous_yaw = 0.0f;
        private double previous_yaw_normalized = 0.0f;
        private double previous_roll_constrained = 0.0f;

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
            // --- Begin variables returned from SimConnect:
            public double latitude;
            public double longitude;
            public double altitude;
            public double pitch;
            public double roll;
            public double heading;
            public double ground_velocity;
            public double center_wheel_rpm;
            public Int64  sim_on_ground;                // strange: can't fetch this variable as an int!?
            public double turn_coordinator_ball;
            public double delta_heading_rate;
            public double elevator_position;
            public double aileron_position;

            public double gForce;
            public double rotationAccelerationBodyX;
            public double rotationAccelerationBodyY;
            public double rotationAccelerationBodyZ;
            public double rotationVelocityBodyX;
            public double rotationVelocityBodyY;
            public double rotationVelocityBodyZ;
            public double accelerationBodyX;
            public double accelerationBodyY;
            public double accelerationBodyZ;
            public double velocityBodyX;
            public double velocityBodyY;
            public double velocityBodyZ;
            public double verticalSpeed;
            public double variometerRate;
            public double totalWeightYawMOI;
            public double planePitchRadians;
            public double planeBankRadians;
            public double planeHeadingDegreesGyro;
            public double planeHeadingDegreesMagnetic;

            public double airspeedTrue;
            public double aircraftWindX;
            public double aircraftWindY;
            public double aircraftWindZ;
            public double relativeWindVelocityBodyX;
            public double relativeWindVelocityBodyY;
            public double relativeWindVelocityBodyZ;

            public double contactPointCompression0;
            public double contactPointCompression1;
            public double contactPointCompression2;
            public double contactPointCompression3;
            public double contactPointCompression4;
            public double contactPointCompression5;
            public int    contactPointIsOnGround0;
            public int    contactPointIsOnGround1;
            public int    contactPointIsOnGround2;
            public int    contactPointIsOnGround3;
            public int    contactPointIsOnGround4;
            public int    contactPointIsOnGround5;

            public double engine_rpm_1;
            public double engine_rpm_2;
            public double engine_rpm_3;
            public double engine_rpm_4;
            public double engineVibration1;
            public double engineVibration2;
            public double engineVibration3;
            public double engineVibration4;
            public double engRotorRpm1;
            public double engRotorRpm2;
            public double engRotorRpm3;
            public double engRotorRpm4;
            public int    engineType;
            public int    engineCount;
            public int    simPauseState;        // initially fetches CAMERA STATE, converted to pause state

            // --- End variables returned from SimConnect (66 currently)

            // --- Begin McFredward's additional computed fields
            public double pitch_multiplier;
            public double roll_normalized;
            public double yaw_normalized;
            public double yaw_normalized_twist;

            // --- Begin CCrim's additional computed fields
            public double roll_dynamic;
            public double pitch_dynamic;
            public double yaw_dynamic;
         };

        // Field index offsets for input variables that are not zeroed when pasued:
        const int kHeading = 5;
        const int kSimPauseState = 66;
        const int kYawNormalized = 69;
        const int kYawNormalizedTwist = 70;

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

        // Returns array of available input variable names.
        public string[] GetInputData()
        {
            Type f = typeof(Struct1);
            FieldInfo[] fields = f.GetFields();

            string[] inputs = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                inputs[i] = fields[i].Name;
            }

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

                InputEvents(simconnect);

                // define a data structure
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE LATITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE LONGITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE ALTITUDE", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.00f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.00f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE HEADING DEGREES TRUE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.00f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GROUND VELOCITY", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CENTER WHEEL RPM", "rpm", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "SIM ON GROUND", "bool", SIMCONNECT_DATATYPE.INT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "TURN COORDINATOR BALL", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "DELTA HEADING RATE", "radians per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ELEVATOR POSITION", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AILERON POSITION", "position", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "G FORCE", "G force", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION ACCELERATION BODY X", "degrees per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION ACCELERATION BODY Y", "degrees per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION ACCELERATION BODY Z", "degrees per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY X", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY Y", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY Z", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ACCELERATION BODY X", "feet per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ACCELERATION BODY Y", "feet per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ACCELERATION BODY Z", "feet per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "VELOCITY BODY X", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "VELOCITY BODY Y", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "VELOCITY BODY Z", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "VERTICAL SPEED", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "VARIOMETER RATE", "feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "TOTAL WEIGHT YAW MOI", "slugs feet squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE PITCH DEGREES", "radians", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE BANK DEGREES", "radians", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE HEADING DEGREES GYRO", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE HEADING DEGREES MAGNETIC", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AIRSPEED TRUE", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AIRCRAFT WIND X", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AIRCRAFT WIND Y", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AIRCRAFT WIND Z", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RELATIVE WIND VELOCITY BODY X", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RELATIVE WIND VELOCITY BODY Y", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RELATIVE WIND VELOCITY BODY Z", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
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

                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG RPM:1", "RPM", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG RPM:2", "RPM", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG RPM:3", "RPM", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG RPM:4", "RPM", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG VIBRATION:1", "number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG VIBRATION:2", "number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG VIBRATION:3", "number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG VIBRATION:4", "number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG ROTOR RPM:1", "percent scaler 16k", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG ROTOR RPM:2", "percent scaler 16k", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG ROTOR RPM:3", "percent scaler 16k", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG ROTOR RPM:4", "percent scaler 16k", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENGINE TYPE", "enum", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "NUMBER OF ENGINES", "number", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CAMERA STATE", "enum", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                // IMPORTANT: register it with the simconnect managed wrapper marshaller
                // if you skip this step, you will only receive a uint in the .dwData field.
                simconnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);

                // catch a simobject data request
                simconnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(simconnect_OnRecvSimobjectDataBytype);

                // Subscribe to SIM_START and SIM_END to determine if the simulation is paused:
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
                simPauseState = PAUSE_STATE_FLAG_OFF;
                isSimRunning = true;
                Debug.WriteLine("Simulation started.");
            }
            else if ((SimEvent)data.uEventID == SimEvent.SIM_STOP)
            {
                simPauseState = PAUSE_STATE_FLAG_PAUSE;
                isSimRunning = false;
                Debug.WriteLine("Simulation stopped.");
            }
            else if ((SimEvent)data.uEventID == SimEvent.PAUSE_EX1)
            {
                simPauseState = (int)data.dwData;
                isSimRunning = (simPauseState == PAUSE_STATE_FLAG_OFF);
                HandlePauseState(simPauseState);
            }
        }

        // Handle detailed pause state
        private void HandlePauseState(int pauseState)
        {
            if (pauseState == PAUSE_STATE_FLAG_OFF)              // No pause
            {
                Debug.WriteLine("Simulation resumed.");
            }
            else if (pauseState == PAUSE_STATE_FLAG_PAUSE)      // Full pause
            {
                Debug.WriteLine("Simulation fully paused.");
            }
            else if (pauseState == PAUSE_STATE_FLAG_ACTIVE_PAUSE)    // Active pause
            {
                Debug.WriteLine("Simulation in active pause.");
            }
            else if (pauseState == PAUSE_STATE_FLAG_SIM_PAUSE)   // Sim paused, but other elements still running
            {
                Debug.WriteLine("Simulation paused, but traffic and multiplayer are running.");
            }
            else
            {
                Debug.WriteLine($"Unknown pause state: {pauseState}");
            }
        }

        private void CheckCameraStateForPause(ref Struct1 s1)
        {
            // The MSFS pause flags don't report when we're in the main menu or still loading the flight.
            // Some folks have worked out that you can use the CAMERA STATE variable to detect other times that a flight is not active.
            // So we're loading that variable in s1.simPauseState and then testing whether to overwrite it with the known pause state.
            if (s1.simPauseState < 11)
            {
                s1.simPauseState = simPauseState;   // camera is in a flight mode: replace with known pause state
                isSimRunning = (simPauseState == PAUSE_STATE_FLAG_OFF);
            }
            else if (isSimRunning)
            {
                Debug.WriteLine("Simulation in non-flight camera state (treating as paused).");
                s1.simPauseState = simPauseState;
                isSimRunning = false;
            }
        }

        // Useful constants
        const double kGravity = 32.174;         // feet/sec^2 to match accelerationBody units
        const double kConvertToDegrees = 180.0 / Math.PI;
        const double kConvertToRadians = Math.PI / 180.0;

        private double NormalizeAngle(double angle)
        {
            // Bring the angle within the range [-180, 180]
            while (angle > 180.0f) angle -= 360.0f;
            while (angle < -180.0f) angle += 360.0f;
            return angle;
        }

        private double ConstrainAngle(double angle, double pivotAngle = 90)
        {
            // Linearly constrain roll/bank angle around 90 degree pivot:
            if (Math.Abs(angle) > 90)
            {
                angle = Math.Sign(angle) * (180 - Math.Abs(angle));
            }

            // Alternate methods supporting arbitrary angles...
            // Constrain roll angle with sinusoidal or linear easing back to 0 after pivot point:
/*          if (Math.Abs(angle) > pivotAngle)
            {
                double excessAngle = Math.Abs(angle) - pivotAngle;

                // Apply cosine easing to the excess angle and restore the sign:
                //angle = Math.Sign(angle) * pivotAngle * Math.Cos(Math.PI * excessAngle / (2 * (180 - pivotAngle)));

                // Linear version:
                //angle = Math.Sign(angle) * (pivotAngle - excessAngle * (pivotAngle / (180 - pivotAngle)));
            }
*/
            return angle;
        }

        private void CalcExtraSimVariables(ref Struct1 s1)
        {
            double roll_constrained = ConstrainAngle(s1.roll);

            // --- Begin McFredward's additional computed fields (pitch_multiplier, roll_normalized, yaw_normalized, yaw_normalized_twist)

            // Reduce roll angle by pitch (going to 0 at 90 degree pitch, reduced by about 30% at 45 degrees):
            s1.pitch_multiplier = Math.Cos(s1.planePitchRadians);
            s1.roll_normalized = s1.pitch_multiplier * roll_constrained;

            // Fix tipping point problem with raw yaw values:
            double yaw = s1.heading;       // using heading for yaw_normalized
            double yaw_delta = (s1.pitch_multiplier * NormalizeAngle(previous_yaw - yaw));
            s1.yaw_normalized = NormalizeAngle(previous_yaw_normalized - yaw_delta);
            s1.yaw_normalized_twist = NormalizeAngle(s1.yaw_normalized - (yaw_delta + (1 - s1.pitch_multiplier) * NormalizeAngle(previous_roll_constrained - roll_constrained)));

            previous_yaw = yaw;
            previous_yaw_normalized = s1.yaw_normalized;
            previous_roll_constrained = roll_constrained;

            // --- End McFredward's additional computed fields


            // --- Begin CCrim's additional computed fields

    /*      This section calculates roll_dynamic and pitch_dynamic variables to provide roll and pitch tilt angles to simulate the forces
            felt in a real aircraft.

            Background: In a normal coordinated turn, gravity, lift and centrifugal forces balance and combine in such a way to direct
            an increased g force directly perpindicular to the seat. When the forces are balanced, the pilot/ passenger feels no sideways force
            from either the bank angle or centrifugal force of the turn.Instead they feel a force pressing them into their seat, proportional
            to the strength of the turn(determined by airspeed and turn radius).

            During uncoordinated turns, the opposing forces are unbalanced, and you would feel a force directed to either the inside or outside
            of the turn depending on the orientation of the plane-- with the nose pointing into or away from the inside of the turning circle.
            Aircraft feature a device(a turn/ slip indicator or turn coordinator ball) to help pilots maintain coordinated turns
            and perform standard rate turns(like two minutes for 360 degrees).

            In a skid, the nose is pointed to the inside the turn, you feel a centrifugal pull to the outside of the turn and the turn coordinator
            ball moves to the outside.In a sideslip, the nose is pointed to the outside of the turn, you feel a force pulling to the inside
            of the turn and the turn coordinator ball moves to the inside.

            Similarly, in an aircraft that can maintain a static roll (roll attitude without turning), the forces are again unbalanced and you feel
            the force of gravity and the counterforce of lift to your side.

            A motion sim rig can thus simulate these unbalanced forces by tilting to the inside or outside of the turn direction along the roll axis
            in proportion to the amount of force but scaled down as very little tilt is actually needed to be convincing when your view also
            mimics the banking motion via VR goggles or an enclosed cockpit.

            Axis and Variable Orientation:                                                      left    right
                x axis: lateral         positive is right                    accelerationBodyX:   -       +
                y axis: vertical        positive is up                   turn_coordinator_ball:   -       +
                z axis: longitudinal    positive is forward          roll/bank, incidence_beta:   +       -
                                        motion rig pitch up/back: negative, roll right: negative

            To simulate force from slip or skid, we always want to tilt in the direction of the ball or opposite incidence_beta's sign.
            Turning to left example: accelerationBodyX goes negative.  When it's coordinated, no tilt.
            Without left rudder or with too much right rudder, coordinator ball goes to left, we want to tilt left.
            With too much rudder left, ball goes to the right, we want to tilt right to simulate centrifugal force.
            The TURN INDICATOR BALL variable isn't ideal because it's simulating a mechanical device with dampening (too slow)
            and too much oscillation -- it'll swing from side to side for several seconds in fast maneuvers.
    */

            // I've tried so many ways to somehow separate or calculate the coordinated vs uncoordinated acceleration forces from each other
            // but everything leads to roadblocks or discontinuities or lag or bad mixing between centripetal and lift/gravity components.
            // Nothing works well enough. I've saved the code from all those attempts separately but I'm giving up for now and
            // just using the raw acceleration variable in the profile...

            // This is a compromise solution because the turn corodinator ball is dampened (simulating ball in oil) and it will oscillate
            // in large movements across its range with large maneuvers and takes a couple seconds to settle down. 
            s1.roll_dynamic = Math.Abs(s1.accelerationBodyX) * s1.turn_coordinator_ball;


            // Calculate a value for the pitch axis:
            // Gravity (1G) is not included in accelerationBodyY (it's 0 in level flight) and we're in a 1G environment already so let's just
            // account for the vertical component of gravity shifting forward/back by pitch angle and decreasing by bank angle.
            // We'll mix in the vertical acceleration (accelerationBodyY) in the profile separately.
            double verticalRatio = Math.Sin(s1.planePitchRadians);
            double bankInfluence = Math.Abs(Math.Cos(s1.planeBankRadians));

            // Blend the bank angle influence -- full effect when level to no effect when vertical:
            double blendedBankInfluence = 1.0 + (bankInfluence - 1.0) * (1.0 - Math.Abs(verticalRatio));

            // Correct for sign flip with Math.Sin(s1.planePitchRadians) when we exceed +90/-90 degrees pitch:
            if (Math.Abs(s1.pitch) > 90)
                verticalRatio = -verticalRatio;

            s1.pitch_dynamic = verticalRatio * blendedBankInfluence * kGravity;
            // + s1.accelerationBodyY   (leave out: easier to tune separate terms in Game Link)

            // Calculate a value for the yaw axis:
            // Scale the vertical axis rotational acceleration by the "yaw motion of inertia" of the plane since we can't get a measure
            // of the distance for the pilot from the center of rotation.
            // (It's a big range of values: airliner might be 2.8 million "slugs per ft squared" vs. 10,000 for a small plane.)
            double scaleFactor = 1.0 + (0.001 * Math.Pow(s1.totalWeightYawMOI, 0.35));
            s1.yaw_dynamic = s1.rotationAccelerationBodyY / scaleFactor;

            // --- End CCrim's additional computed fields
        }

        private void simconnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.REQUEST_1:
                    // Retrieve the data as the specified structure
                    Struct1 s1 = (Struct1)data.dwData[0];
                    CheckCameraStateForPause(ref s1);
                    CalcExtraSimVariables(ref s1);
                    FieldInfo[] fields = typeof(Struct1).GetFields();

                    // Initialize array if null
                    if (targetValues == null)
                    {
                        targetValues = new float[fields.Length];
                    }

                    // Suppress telemetry while paused/in the menus - still need a way to detect initial loading screen phase though.
                    if (isSimRunning != isSimRunningPrevious)
                    {
                        isSimRunningPrevious = isSimRunning;
                        // Start transitioning to actual data if resumed
                        isTransitioning = true;
                        transitionStartTime = stopwatch.Elapsed.TotalSeconds;
                        initialValues = (float[])targetValues.Clone();
                    }

                    float transitionFactor = 1.0f;
                    if (isTransitioning)
                    {
                        double currentTime = stopwatch.Elapsed.TotalSeconds;
                        double elapsedTime = currentTime - transitionStartTime;

                        // Calculate interpolation factor (0.0 to 1.0)
                        transitionFactor = (float)Math.Min(elapsedTime / transitionDuration, 1.0);
                        isTransitioning = (transitionFactor < 1.0f);
                    }

                    // Load target values if sim is running, otherwise zero everything (w/exceptions):
                    for (int i = 0; i < fields.Length; i++)
                    {
                        // Use current values if sim is running or for heading-based variables (to avoid rotating motion rig):
                        if (isSimRunning || i == kHeading || i == kYawNormalized || i == kYawNormalizedTwist || i == kSimPauseState)
                        {
                            float value = Convert.ToSingle(fields[i].GetValue(s1));
                            targetValues[i] = float.IsNaN(value) ? 0.0f : (float)Math.Round(value, 3);      // ?? why was test for NaN added?
                        }
                        // Otherwise go to zero:
                        else
                        {
                            targetValues[i] = 0.0f;
                        }

                        if (isTransitioning)
                        {
                            // Perform a linear interpolation between initialValues and targetValues:
                            targetValues[i] = initialValues[i] + (targetValues[i] - initialValues[i]) * transitionFactor;
                        }
                        controller.SetInput(i, targetValues[i]);
                    }
                    break;

                default:
                    Debug.WriteLine("Unknown request ID: " + data.dwRequestID);
                    break;
            }
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

        // Esemény azonosítók
        private enum EventID
        {
            MasterBatteryOff = 1000,
        }

        // Bemenet azonosítók
        private enum InputID
        {
            Input0 = 1000,
        }

        private enum GroupID 
        {
            GROUP0 = 1000,
        }

        private enum DownEventID
        {
            DownEventID0 = 1000,
        }

        void InputEvents(SimConnect simconnect) 
        {
            Debug.WriteLine("InputEvents..");
            // Esemény beállítása
            simconnect.MapClientEventToSimEvent(EventID.MasterBatteryOff, "MASTER_BATTERY_OFF");

            // Esemény hozzáadása a csoporthoz
            simconnect.AddClientEventToNotificationGroup(GroupID.GROUP0, EventID.MasterBatteryOff, false);

            // Csoport prioritás beállítása
            //simconnect.SetNotificationGroupPriority(GroupID.GROUP0, SimConnectGroupPriority.Highest);

            // Input esemény és az esemény hozzárendelése
            simconnect.MapInputEventToClientEvent(InputID.Input0, "z", EventID.MasterBatteryOff, 0, DownEventID.DownEventID0, 0,false);

            // Bemeneti csoport aktiválása
            simconnect.SetInputGroupState(InputID.Input0, 1);
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

        public Type GetConfigBody()
        {
            return null;
        }
    }
}
