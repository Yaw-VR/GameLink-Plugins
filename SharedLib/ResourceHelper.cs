﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharedLib
{
    internal class ResourceHelper
    {
        /// <summary>
        /// Retrieves a stream for the specified embedded resource.
        /// </summary>
        /// <param name="resourceName">The name of the resource to retrieve.</param>
        /// <returns>A <see cref="Stream"/> for the specified resource, or <c>null</c> if the resource is not found.</returns>
        public static Stream GetStream(string resourceName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var rr = assembly.GetManifestResourceNames();
            string fullResourceName = $"{assembly.GetName().Name}.Resources.{resourceName}";
            return assembly.GetManifestResourceStream(fullResourceName);
        }

        /// <summary>
        /// Retrieves the content of the specified embedded resource as a string.
        /// </summary>
        /// <param name="resourceName">The name of the resource to retrieve.</param>
        /// <returns>A <see cref="string"/> containing the content of the specified resource, or an empty string if the resource is not found.</returns>
        public static string GetString(string resourceName)
        {

            var result = string.Empty;

            using var stream = GetStream(resourceName);

            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                result = reader.ReadToEnd();
            }


            return result;
        }

        public static string GetAssemblyVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            return (assembly?.GetName().Version ?? new Version(0,0)).ToString();
        }
    }
}
