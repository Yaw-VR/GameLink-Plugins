using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YawGLAPI;

namespace SharedLib.GameFinders
{
    internal interface IGameFinder
    {
        public string FindGame(Game plugin, IMainFormDispatcher dispatcher);
        
    }
}
