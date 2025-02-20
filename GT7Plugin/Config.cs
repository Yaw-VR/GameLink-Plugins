using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YawGLAPI;

namespace GT7Plugin
{
    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 33740;

        public Config()
        {
        }
    }
}
