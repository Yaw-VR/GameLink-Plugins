using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;

using YawGLAPI;
#nullable disable

namespace SharedLib.GameFinders
{
    internal class YawGameFinder : IGameFinder
    {
        /// <summary>
        /// Use yaws dispatcher to try to find game uninstall entry from the registry
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public string FindGame(Game plugin, IMainFormDispatcher dispatcher)
        {
            string name = plugin.GetType().GetCustomAttributes<ExportMetadataAttribute>(true)
                .Where(meta => meta.Name == "Name").Select(m => (string)m.Value).First();

            
            string installPath = dispatcher.GetInstallPath(name);
            if (!string.IsNullOrWhiteSpace(installPath) && !Directory.Exists(installPath))
            {
                dispatcher.DialogShow("Can't find install directory\n\nOpen Plugin manager to set it?", DIALOG_TYPE.QUESTION, (yes) =>
                {
                    dispatcher.OpenPluginManager();
                });
                return null;
            }
            return installPath;
        }
    }
}
