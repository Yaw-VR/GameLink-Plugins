using FalconBMS;

using SharedLib;
using SharedLib.TelemetryHelper;

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;

using YawGLAPI;
using F4SharedMem;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json.Linq;


namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Falcon BMS")]
    [ExportMetadata("Version", "0.1")]
    public class Plugin : Game
    {

        #region Standard Properties
        public int STEAM_ID => 0000000; // Its not on Steam
        public string PROCESS_NAME => "Falcon BMS"; // The gameprocess name. App will wait/monitor this process for different features like autostart.

        public bool PATCH_AVAILABLE => false; // Tell app if patch is needed. "Patch" Button will appear -> Needed manually

        public string AUTHOR => "McFredward"; // Creator, will show this on plugin manager

        public string Description => ResourceHelper.Description;
        public Stream Logo => ResourceHelper.Logo;
        public Stream SmallLogo => ResourceHelper.SmallLogo;
        public Stream Background => ResourceHelper.Background;
        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(ResourceHelper.DefaultProfile);

        public LedEffect DefaultLED() => dispatcher.JsonToLED(ResourceHelper.DefaultProfile);

        private IMainFormDispatcher dispatcher; // this is our reference to the app. Features like showing dialog/notification can be used
        private IProfileManager controller; // this is our reference to profile manager. input values need to be passed to this

        /// <summary>
        /// The app will give us these references. We need to save them
        /// </summary>
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }

        #endregion

        private bool running = false; // this is used to stop the read thread
        // For a smooth transition
        private bool isSimRunning = true;
        private bool isSimRunningPrevious = true;
        private float[] targetValues = null;
        private float[] initialValues = null;
        private bool isTransitioning = false;
        private float transitionDuration = 1.5f;
        private double transitionStartTime;
        private Stopwatch stopwatch = Stopwatch.StartNew();

        // For tracking damage
        private float damageYaw = 0.0f;
        private float damagePitch = 0.0f;
        private float damageRoll = 0.0f;
        private float damageRumble = 0.0f;

        private float damageDecayRate = 1.0f; // seconds to decay fully
        private double lastDamageTime = 0.0;
        private int lastRegisteredDamage = 0;
        private float lastRegisteredDamageForce = 0.0f;

        // For acceleration calculations
        private float vtDelta = 0.0f; // Delta VT
        private float previousVt = 0.0f; // Previous VT
        private double prevTime = 0.0f; // Previous time for delta calculations
        float updateInterval = 0.05f; // 50 ms

        // For calculating immersive rotation values
        private float yawImmersive = 0.0f;
        private float previousYaw = 0.0f;
        private float previousYawImmersive = 0.0f;
        float yawImmersiveDelta = 0.0f;

        private static readonly string[] FlightDataFields = new[]
        {
            "xDot", "yDot", "zDot",
            "pitch", "roll", "yaw",
            "gs", "alpha", "beta",
            "turnRate", "bumpIntensity",
            "vt", "kias", "mach",
            "lefPos", "tefPos", "vtolPos",
            "TrimPitch", "TrimRoll", "TrimYaw",
            "rpm", "rpm2"
        };

        private static readonly string[] IntelliVibeDataFields = new[]
        {
            "Gforce", "IsFiringGun", "IsOverG", "IsOnGround"
        };



        public void Init()
        {
            running = true;

            new Thread(ReadThread).Start();
        }

        public void Exit()
        {
            running = false;
        }

        /// <summary>
        /// App fetches available inputs through this
        /// </summary>
        /// <returns></returns>
        public string[] GetInputData()
        {
            return FlightDataFields
                .Concat(IntelliVibeDataFields)
                .Concat(new[] { "adaptedYawDelta", "adaptedPitch", "adaptedRoll", "damageYaw", "damagePitch", 
                    "damageRoll", "damageRumble", "vtDelta" })
                .ToArray();
        }

        /// <summary>
        /// Features for the plugin. 
        /// These features can be setup to be called for different events like pushing arcade buttons
        /// </summary>
        public Dictionary<string, ParameterInfo[]> GetFeatures() => null;

        public Type GetConfigBody() => typeof(Config);

        /// <summary>
        /// Will be called when app starts this plugin
        /// </summary>

        public void ReadThread()
        {
            var flightDataReader = new Reader();
            int inputCount = FlightDataFields.Length + IntelliVibeDataFields.Length + 8; // +8 for adapted values

            while (running)
            {
                try
                {
                    if (!flightDataReader.IsFalconRunning)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    FlightData flightData = flightDataReader.GetCurrentData();

                    isSimRunning = !(flightData.IntellivibeData.IsEndFlight || flightData.IntellivibeData.IsPaused || flightData.IntellivibeData.IsEjecting);

                    if (flightData == null)
                    {
                        Debug.WriteLine("Flight data is null");
                        continue;
                    }

                    double currentTime = stopwatch.Elapsed.TotalSeconds;
                    float deltaTime = (float)(currentTime - prevTime);

                    float rigPitch = flightData.pitch * (180 / (float)Math.PI);
                    float rigRoll = flightData.roll * (180 / (float)Math.PI);
                    float rigYaw = flightData.yaw * (180 / (float)Math.PI);
                    float pitchMultiplier = (float)Math.Cos(rigPitch * Math.PI / 180.0);

                    float rollImmersiveTemp = (rigRoll < -90) ? -180 - rigRoll :
                                                (rigRoll > 90) ? 180 - rigRoll : rigRoll;
                    float rollImmersive = pitchMultiplier * rollImmersiveTemp;
                    yawImmersive = NormalizeAngle(yawImmersive - (pitchMultiplier * NormalizeAngle(previousYaw - rigYaw)));

                    previousYaw = rigYaw;

                    // Detect damage

                    int currentDamage = flightData.IntellivibeData.lastdamage;
                    float currentDamageForce = flightData.IntellivibeData.damageforce;

                    bool isNewDamage = currentDamage != lastRegisteredDamage || currentDamageForce != lastRegisteredDamageForce;

                    if (isNewDamage && currentDamage >= 1 && currentDamage <= 8 && currentDamageForce > 0.01f)
                    {
                        // Store current damage as "already handled"
                        lastRegisteredDamage = currentDamage;
                        lastRegisteredDamageForce = currentDamageForce;

                        lastDamageTime = stopwatch.Elapsed.TotalSeconds;

                        float force = currentDamageForce; // May want to scale

                        // Reset
                        damageYaw = 0;
                        damagePitch = 0;
                        damageRoll = 0;
                        damageRumble = force;

                        switch (currentDamage)
                        {
                            case 1: damagePitch = +force; damageRoll = +force; damageYaw = +force; break;
                            case 2: damagePitch = +force; damageRoll = -force; damageYaw = -force; break;
                            case 3: damagePitch = -force; damageRoll = +force; damageYaw = +force; break;
                            case 4: damagePitch = -force; damageRoll = -force; damageYaw = -force; break;
                            case 5: damageYaw = (float)(new Random().NextDouble() * 2 - 1) * force; break;
                            case 6: damagePitch = +force; damageRumble = force * 1.5f; break;
                            case 7: damageRoll = +force; damageYaw = +force; break;
                            case 8: damageRoll = -force; damageYaw = -force; break;
                        }
                    }

                    // Decay over time
                    double timeSinceDamage = stopwatch.Elapsed.TotalSeconds - lastDamageTime;
                    float decayFactor = (float)Math.Max(0.0, 1.0 - (timeSinceDamage / damageDecayRate));

                    float damageYawOut = damageYaw * decayFactor;
                    float damagePitchOut = damagePitch * decayFactor;
                    float damageRollOut = damageRoll * decayFactor;
                    float damageRumbleOut = damageRumble * decayFactor;

                    if (deltaTime >= updateInterval)
                    {
                        vtDelta = flightData.vt - previousVt;
                        previousVt = flightData.vt;

                        // Also use lower update rate for yaw delta
                        yawImmersiveDelta = yawImmersive - previousYawImmersive;
                        previousYawImmersive = yawImmersive;

                        prevTime = currentTime;
                    }


                    // Prepare full input array
                    float[] fullValues = new float[inputCount];
                    int idx = 0;

                    foreach (var (_, value) in GetInputs(flightData, FlightDataFields))
                        fullValues[idx++] = value;

                    foreach (var (_, value) in GetInputs(flightData.IntellivibeData, IntelliVibeDataFields))
                        fullValues[idx++] = value;

                    float yawIndex = idx;
                    fullValues[idx++] = yawImmersiveDelta;
                    fullValues[idx++] = rigPitch;
                    fullValues[idx++] = rollImmersive;
                    fullValues[idx++] = damageYawOut;
                    fullValues[idx++] = damagePitchOut;
                    fullValues[idx++] = damageRollOut;
                    fullValues[idx++] = damageRumbleOut;
                    fullValues[idx++] = vtDelta;

                    // Initialize target arrays if needed
                    if (targetValues == null)
                    {
                        targetValues = new float[inputCount];
                        initialValues = new float[inputCount];
                    }

                    // Detect pause/resume transition
                    if (isSimRunning != isSimRunningPrevious)
                    {
                        isSimRunningPrevious = isSimRunning;
                        isTransitioning = true;
                        transitionStartTime = stopwatch.Elapsed.TotalSeconds;
                        Array.Copy(targetValues, initialValues, inputCount); // Save current as initial
                    }

                    float transitionFactor = 1.0f;
                    if (isTransitioning)
                    {
                        double elapsedTime = stopwatch.Elapsed.TotalSeconds - transitionStartTime;
                        transitionFactor = (float)Math.Min(elapsedTime / transitionDuration, 1.0);
                        isTransitioning = (transitionFactor < 1.0f);
                    }

                    for (int i = 0; i < inputCount; i++)
                    {
                        if (!isSimRunning && i != yawIndex) // Zero flight data, keep state inputs
                        {
                            targetValues[i] = 0.0f;
                        }
                        else
                        {
                            targetValues[i] = fullValues[i];
                        }

                        // Apply transition
                        if (isTransitioning)
                        {
                            targetValues[i] = initialValues[i] + (targetValues[i] - initialValues[i]) * transitionFactor;
                        }

                        controller.SetInput(i, targetValues[i]);
                    }
                }
                catch (SocketException ex)
                {
                    Debug.WriteLine($"Socket error: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        public void PatchGame()
        {
        }

        public static IEnumerable<(string key, float value)> GetInputs(object data, string[] fieldNames)
        {
            if (data == null || fieldNames == null)
                yield break;

            var type = data.GetType();
            var fieldMap = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                               .ToDictionary(f => f.Name, f => f);

            foreach (var name in fieldNames)
            {
                if (fieldMap.TryGetValue(name, out var field))
                {
                    object val = field.GetValue(data);
                    if (val == null) continue;

                    if (field.FieldType == typeof(float))
                        yield return (name, (float)val);
                    else if (field.FieldType == typeof(int))
                        yield return (name, Convert.ToSingle((int)val));
                    else if (field.FieldType == typeof(uint))
                        yield return (name, Convert.ToSingle((uint)val));
                    else if (field.FieldType == typeof(double))
                        yield return (name, Convert.ToSingle((double)val));
                    else if (field.FieldType == typeof(bool))
                        yield return (name, (bool)val ? 1.0f : 0.0f);
                    else if (field.FieldType.IsEnum)
                        yield return (name, Convert.ToSingle((int)val));
                }
            }
        }

        private float NormalizeAngle(float angle)
        {
            // Bring the angle within the range [-180, 180]
            while (angle > 180.0f) angle -= 360.0f;
            while (angle < -180.0f) angle += 360.0f;
            return angle;
        }


    }

}
