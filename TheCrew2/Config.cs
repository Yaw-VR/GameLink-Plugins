using YawGLAPI;

namespace TheCrew2
{

    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 1337;

        public Config()
        {
        }
    }


}
