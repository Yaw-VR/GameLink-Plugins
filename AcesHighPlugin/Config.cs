using YawGLAPI;

namespace AcesHighPlugin
{
    public struct Config
    {
        [Info(Description = "Port the game sends data on")]
        public int port = 556;

        public Config()
        {
        }
    }
  
}
