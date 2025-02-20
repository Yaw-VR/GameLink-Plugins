using YawGLAPI;

namespace VRaceHoverBikePlugin
{
    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 8051;
        [Info(Description = "Sending frequency")]
        public int RefreshRate = 20;

        public Config()
        {
        }
    }


}
