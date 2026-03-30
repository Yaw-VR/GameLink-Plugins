using YawGLAPI;

namespace KayakVRMiragePlugin
{
    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 33001;

        public Config()
        {
        }
    }
}
