﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ProjectCars3Plugin.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ProjectCars3Plugin.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {&quot;GameName&quot;:&quot;Project Cars 3&quot;,&quot;Name&quot;:&quot;projectcarsuj&quot;,&quot;components&quot;:[{&quot;Constant&quot;:false,&quot;Input_index&quot;:0,&quot;Output_index&quot;:0,&quot;MultiplierPos&quot;:1.0,&quot;MultiplierNeg&quot;:1.0,&quot;Offset&quot;:0.0,&quot;Inverse&quot;:false,&quot;Limit&quot;:-1.0,&quot;Smoothing&quot;:1.0,&quot;Enabled&quot;:true,&quot;Spikeflatter&quot;:{&quot;Enabled&quot;:false,&quot;Limit&quot;:100.0,&quot;Strength&quot;:0.5},&quot;Deadzone&quot;:0.0,&quot;Type&quot;:0,&quot;Condition&quot;:[],&quot;Math&quot;:[]},{&quot;Constant&quot;:false,&quot;Input_index&quot;:1,&quot;Output_index&quot;:1,&quot;MultiplierPos&quot;:1.0,&quot;MultiplierNeg&quot;:1.0,&quot;Offset&quot;:0.0,&quot;Inverse&quot;:true,&quot;Limit&quot;:-1.0,&quot;Smoothing&quot;:1.0,&quot;Enabled&quot;:true,&quot;Spikef [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string defProfile {
            get {
                return ResourceManager.GetString("defProfile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to To enable Project Cars 3 telemetry: 
        ///&lt;br&gt;
        ///Go into Options-&gt;System and set UDP frequency to 2, and protocol version to project CARS 2.
        /// </summary>
        internal static string description {
            get {
                return ResourceManager.GetString("description", resourceCulture);
            }
        }
    }
}