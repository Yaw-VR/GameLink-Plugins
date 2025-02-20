using YawGLAPI;

namespace KartKraftPlugin
{

    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 5000;

        public Config()
        {
        }
    }


}
