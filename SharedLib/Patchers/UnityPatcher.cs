using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using YawGLAPI;

#nullable disable

namespace SharedLib
{
    /// <summary>
    /// Patch options for Unity games
    /// </summary>
    internal class UnityPatcherOptions: PatcherOptions
    {
        /// <summary>
        /// Path relative to the game install path where the doorstop_config.ini file is located.
        /// </summary>
        public string DoorStopPath { get; set; } = "";

        public bool InstallConfigurationManager { get; set; }

    }


    /// <summary>
    /// Patcher for Unity games
    /// </summary>
    internal class UnityPatcher : Patcher<UnityPatcherOptions>
    {
        private string _doorStopIni => PathCombine(options.DoorStopPath, "doorstop_config.ini");

        protected override TaskList Tasks
        {
            get
            {
                try
                {
                    if(options.InstallConfigurationManager)
                    {
                        return new()
                        {
                            { "Install BepIn Ex",InstallBepInExTask},
                            { "Install Configuration Manager", InstallBepInConfigManagerTask},
                            { "Find BepInEx Plugins Dir",FindBepInExPluginsDirTask},
                            { "Reinstall If Plugin Found Async",AskReinstallTask},
                            { "Install Mod",InstallModTask}
                        };
                    }
                    
                    return new()
                    {
                        { "Install BepIn Ex",InstallBepInExTask},                    
                        { "Find BepInEx Plugins Dir",FindBepInExPluginsDirTask},
                        { "Reinstall If Plugin Found Async",AskReinstallTask},
                        { "Install Mod",InstallModTask}
                    };
                }
                catch (Exception e)
                {
                    Log("Error: " + e.Message);
                    return null;

                }
            }
        }


        string modFolder = null;

        List<GithubAsset> downloads = [];

        readonly Dictionary<ModType, (string version, string bitness)> modversion = new()
        {
            { ModType.BepInEx5_x64 , ("v5.4.23.2", "win_x64")}
        };

        public UnityPatcher(string installPath, IMainFormDispatcher dispatcher, UnityPatcherOptions options) : base(installPath, dispatcher, options)
        {
        }

        #region Tasks

        private async Task<bool> InstallBepInExTask(CancellationToken cancellationToken = default)
        {
            Log("Installing BepInEx...");
            try
            {
                var doorStopIniFile = PathCombine(this.InstallPath, _doorStopIni);

                if (!File.Exists(doorStopIniFile))
                {
                    switch (options.ModType)
                    {
                        case ModType.RaiPal:
                            Feedback( false, "Please install UUVR with RaiPal first");
                           
                            return false;
                        case ModType.BepInEx5_x64:
                        {
                            using var bepinex = GithubClient.Create(o =>
                            {
                                o.CachingEnabled = true;
                                o.UsernameOrOrganization = "BepInEx";
                                o.Repository = "BepInEx";
                            });


                            var assets = await bepinex.DownloadByTagAsync(
                                modversion[options.ModType].version, 
                                [modversion[options.ModType].bitness], cancellationToken);

                            foreach (var asset in assets)  
                            {
                                await ExtractFiles(asset.Location, this.InstallPath, true); 
                            }
                            break;
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Log("Error: " + e.Message);
                return false;
            }

            return true;


        }

        private async Task<bool> InstallBepInConfigManagerTask(CancellationToken cancellationToken = default)
        {
            Log("Installing BepInEx...");
            try
            {
                var doorStopIniFile = PathCombine(this.InstallPath, _doorStopIni);

                if (File.Exists(doorStopIniFile))
                {
                    switch (options.ModType)
                    {
                        //case ModType.RaiPal:
                        //    Install Configuration Manager through RaiPal

                        //    return false;
                        case ModType.BepInEx5_x64:
                            {
                                if (!Directory.Exists(PathCombine(this.InstallPath, "BepInEx/plugins/ConfigurationManager")))
                                {
                                    using var bepinex = GithubClient.Create(o =>
                                    {
                                        o.CachingEnabled = true;
                                        o.UsernameOrOrganization = "BepInEx";
                                        o.Repository = "BepInEx.ConfigurationManager";
                                    });


                                    var assets = await bepinex.DownloadLatestAsync(["BepInEx5"], cancellationToken);

                                    if (assets.Count != 1)
                                    {
                                        throw new Exception("Unexpected number of assets found.");
                                    }

                                    await ExtractFiles(assets.Single().Location, this.InstallPath, true);
                                }

                                break;
                            }

                    }
                }
                else
                {
                    Log("BepInEx not found. Cancelling installation.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Log("Error: " + e.Message);
                return false;
            }

            return true;


        }

        private async Task<bool> FindBepInExPluginsDirTask(CancellationToken cancellationToken = default)
        {
            var doorStopIni = await IniHelper.LoadAsync(PathCombine(this.InstallPath, this._doorStopIni));

            string targetAssembly = doorStopIni["General","target_assembly"] ?? doorStopIni["UnityDoorstop","targetAssembly"];

            if (string.IsNullOrWhiteSpace(targetAssembly))
            {
                Feedback(false, "target_assembly not found in doorstop_config.ini");
                return false;
            }

            
            var m = Regex.Match(targetAssembly, @"(?<modFolder>.*?BepInEx)[\\/]");

            if (m.Success)
            {
                modFolder = m.Groups["modFolder"].Value;
                modFolder = Path.Combine(!Regex.IsMatch(modFolder, @"^[a-zA-Z]:[\\/]") ? this.InstallPath : "", modFolder, "plugins/" + options.PluginName).Replace('\\', '/');
            }
            else
            {
                Feedback(false, $"Can't find BepInEx folder inside of \"{targetAssembly}\"");
                return false;
            }

            return true;
        }

        private Task<bool> AskReinstallTask(CancellationToken cancellationToken = default)
        {
            bool quit = false;
            if (Directory.Exists(modFolder))
            {
                bool reinstall = AskQuestion("Mod already installed. Reinstall?");

                if (reinstall)
                {
                    try
                    {
                        Directory.Delete(modFolder, true);
                    }
                    catch (Exception e)
                    {
                        Feedback(false, "Error deleting existing mod: " + e.Message);
                        quit = true;
                    }
                }
                else
                {
                    quit = true;
                }
            }

            return Task.FromResult(!quit);
               
        }

        private async Task<bool> InstallModTask(CancellationToken cancellationToken = default)
        {
            Log("Installing plugin...");
            try
            {
                using var github = GithubClient.Create(o =>
                {
                    o.CachingEnabled = true;
                    o.UsernameOrOrganization = options.Repository.UsernameOrOrganization;
                    o.Repository = options.Repository.Repository;
                });

                downloads = await github.DownloadLatestAsync(cancellationToken: cancellationToken);

                if(downloads.Count == 0)
                {
                    Feedback(false, "No release found.");
                    return false;
                }

                Log($"Downloaded plugin.");

                return await InstallPluginAsync(cancellationToken);
            }
            catch (Exception e)
            {
                Feedback(false, "Error extracting files: " + e.Message);
            }
            return false;
        }

        private Task<bool> InstallPluginAsync(CancellationToken cancellationToken = default)
        {
            Log("Installing plugin...");
            try
            {
                
                foreach (var asset in downloads)
                {
                    var ext = Path.GetExtension(asset.Name);
                    switch(ext)
                    {
                        case ".zip":
                            ExtractFiles(asset.Location, modFolder, true);
                            break;
                        case ".dll":
                            FileCopy(asset.Location, PathCombine(modFolder, asset.Name), true);
                            break;
                        default:
                            Feedback(false, $"Unsupported file type: {asset.Name}");
                            return Task.FromResult(false);
                           
                    }
                                     
                }
                Log($"{downloads.Count} Plugins installed.");
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
               Feedback(false, "Error installing mod files: " + e.Message);
            }
            return Task.FromResult(false);
        }

        #endregion
    }
}
