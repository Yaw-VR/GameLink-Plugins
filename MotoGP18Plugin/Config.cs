using YawGLAPI;

namespace MotoGP18Plugin
{

    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 7100;

        public Config()
        {
        }
    }


}
