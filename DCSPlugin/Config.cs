using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YawGLAPI;

namespace DCSPlugin
{
    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 41230;

        public Config()
        {
        }
    }
}
