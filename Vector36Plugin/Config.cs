using YawGLAPI;

namespace Vector36Plugin
{
    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 4123;

        public Config()
        {
        }
    }


}
