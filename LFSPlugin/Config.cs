using YawGLAPI;

namespace LFSPlugin
{
    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 3156;

        public Config()
        {
        }
    }


}
