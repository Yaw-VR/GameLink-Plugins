using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    public struct Config
    {
        [Info(Description = "IP Address of the device (default 0.0.0.0)", Name = "IP Address", RegexValidator = ConfigValidator.IPValidator)]
        public string IP = "0.0.0.0";

        [Info(Description = "Port the game sends data on (12345)", Name = "Port", RegexValidator = ConfigValidator.PortRange)]
        public int Port = 12345;

        
        public Config() { }

    }


}