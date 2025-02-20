using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YawGLAPI;

namespace ForzaHorizon4Plugin
{
    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 20127;

        public Config()
        {
        }
    }

}
