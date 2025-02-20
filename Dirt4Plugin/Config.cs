using YawGLAPI;

namespace Dirt4Plugin
{
    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 20777;

        public Config()
        {
        }
    }
}
