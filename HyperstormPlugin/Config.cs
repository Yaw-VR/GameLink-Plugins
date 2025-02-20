using YawGLAPI;

namespace HyperstormPlugin
{
    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 20741;

        public Config()
        {
        }
    }
}
