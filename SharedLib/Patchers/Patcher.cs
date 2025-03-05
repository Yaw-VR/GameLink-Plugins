using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using YawGLAPI;

#nullable disable

namespace SharedLib
{
    internal enum ModType
    {
        BepInEx5_x64,
        //BepInEx6_x64,
        RaiPal
    }

    internal class  PatcherOptions
    {
        public ModType ModType { get; set; }

        //public string GameInstallPath { get; set; }

        public string PluginName { get; set; }

        public GithubOptions  Repository { get; set; }        

    }

    internal delegate Task<bool> PatcherTask(CancellationToken cancellation);

    internal class TaskList : Dictionary<string, PatcherTask>
    {      
        public new TaskList Add(string name, PatcherTask task)
        {
            base.Add(name, task);
            return this;
        }
    }

    internal interface IPatcher
    {
        Task<bool> PatchAsync(CancellationToken cancellationToken = default);
    }

    internal abstract class Patcher<TOptions> : IPatcher where TOptions : PatcherOptions, new()
    {
        protected readonly IMainFormDispatcher dispatcher;

        /// <summary>
        /// Options for the patcher
        /// </summary>
        protected readonly TOptions options;

        /// <summary>
        /// List if task functions to run in order. Returning false will stop the patching process.
        /// </summary>
        protected virtual TaskList Tasks => new();

        public string InstallPath { get; }

        public static IPatcher Create<T>(Game plugin, IMainFormDispatcher dispatcher, Action<TOptions> optionsAction) where T : Patcher<TOptions>
        {
            var options = new TOptions();

            optionsAction?.Invoke(options);

            string installPath = GetInstallPath(plugin, dispatcher);

            return Activator.CreateInstance(typeof(T), installPath, dispatcher, options) as T;
        }

        protected Patcher(string installPath, IMainFormDispatcher dispatcher, TOptions options)
        {
            InstallPath = installPath;
            this.dispatcher = dispatcher;
            this.options = options;
        }


        protected void Feedback(bool success, string message)
        {
            Log($"{(success ? "" : "ERROR")}: {message}");
            dispatcher.DialogShow(message, DIALOG_TYPE.INFO);
        }

        public bool AskQuestion(string message)
        {
            bool result = false;
            dispatcher.DialogShow(message, DIALOG_TYPE.QUESTION, (yes) => result = true, no => result = false);
            return result;
        }

        protected Task ExtractFiles(string source, string destination, bool overwrite = true)
        {
            Log($"Extract {Path.GetFileName(source)} to {destination}");
            dispatcher.ExtractToDirectory(source, destination, overwrite);

            return Task.CompletedTask;
        }



       

        async Task<bool> IPatcher.PatchAsync(CancellationToken cancellationToken)
        {
            Log("Patching...");

            bool success = await this.RunTasks(cancellationToken);

            if (success)
                Feedback(true, "Patching complete.");
            else
                Feedback(false, "Patching Aborted.");

            return success;
        }


        //protected abstract Task<bool> RunTasks(CancellationToken cancellationToken = default);

        private async Task<bool> RunTasks(CancellationToken cancellationToken = default)
        {

            foreach (var (name, taskFunction) in this.Tasks)
            {
                Log($"Running task: [{name}]");
                if (!await taskFunction(cancellationToken))
                {
                    Log($"False task: [{name}]");
                    return false;
                }

                Log($"Complete task: [{name}]");
            }

            return true;

        }

        private static string GetInstallPath(Game plugin, IMainFormDispatcher dispatcher) 
        {
            string name = plugin.GetType().GetCustomAttributes<ExportMetadataAttribute>(true)
                .Where(meta => meta.Name == "Name").Select(m => (string)m.Value).First();

            string installPath = dispatcher.GetInstallPath(name);
            if (!string.IsNullOrWhiteSpace(installPath) && !Directory.Exists(installPath))
            {
                dispatcher.DialogShow("Cant find Distance install directory\n\n" + installPath + "\n\nOpen Plugin manager to set it?", DIALOG_TYPE.QUESTION, (yes) =>
                {
                    dispatcher.OpenPluginManager();
                });
                return null;
            }
            return installPath;
        }

        protected string PathCombine(params string[] segments)
        {
            return Path.Combine(segments).Replace("\\", "/");
        }

        protected void Log(string message)
        {
            Console.WriteLine(message);
        }

    }
    

    
}