using YawGLAPI;

namespace FalconBMS
{
    public struct Config
    {
        [Info(Description = "IP Address of the device (default 0.0.0.0)", Name = "IP Address", RegexValidator = ConfigValidator.IPValidator)]
        public string IP = "0.0.0.0";

        [Info(Description = "Port the game sends data on (default 6969)", Name = "Port", RegexValidator = SharedLib.Validator.PORT)]
        public int Port = 6969;

        [Info(Description = "IP Address of the incoming telemetry (default 127.0.0.1)", Name = "Remote IP Address", RegexValidator = ConfigValidator.IPValidator)]
        public string RemoteIP = "127.0.0.1";
        public Config() { }

    }
}
