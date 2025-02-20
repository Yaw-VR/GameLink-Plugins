using YawGLAPI;

namespace ProjectCars2Plugin
{

    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 5606;

        public Config()
        {
        }
    }


}
