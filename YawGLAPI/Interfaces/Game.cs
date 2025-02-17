using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace YawGLAPI
{

    /// <summary>
    /// Plugins have to inherit from Game
    /// </summary>
    public interface Game
        {
            public int STEAM_ID { get; }
            public string PROCESS_NAME { get; }
            public bool PATCH_AVAILABLE { get; }

            public string AUTHOR { get; }

        public Stream Logo { get; }            /// <summary>
        public Stream SmallLogo { get; }            /// <summary>
        public Stream Background { get; }            /// <summary>
        public string Description { get; }            /// <summary>
            /// Called when plugin stopped
            /// </summary>
            public abstract void Exit();

            /// <summary>
            /// Called when plugin started
            /// </summary>
            public abstract void Init();
            /// <summary>
            /// Give instances to the Plugin
            /// </summary>
            public abstract void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher);

            /// <summary>
            /// Returns data provided by plugin
            /// </summary>
            public abstract string[] GetInputData();

            /// <summary>
            /// Default axis settings for plugin
            /// </summary>

            public abstract List<Profile_Component> DefaultProfile();
        /// <summary>
        /// Default LED settings
        /// </summary>
        public abstract LedEffect DefaultLED();

        /// <summary>
        /// Get Features
        /// </summary>
        public abstract Dictionary<string, ParameterInfo[]> GetFeatures();

        /// <summary>
        /// Fetch ConfigBody of the plugin. Only struct is allowed, with primitive type fields ( and string )
        /// </summary>
        /// <returns></returns>
        public abstract Type GetConfigBody();

        /// <summary>
        /// Do neccessary configuration for the game to work
        /// </summary>
        public abstract void PatchGame();
        }
        public interface GameMetaData
        {
            string Name { get; }
            string Version { get; }
        }
    
}
