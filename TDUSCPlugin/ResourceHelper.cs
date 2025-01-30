using System.Diagnostics;
using System.Reflection;

namespace TDUSCPlugin
{
    internal class ResourceHelper
    {
        public static Stream GetStream(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var rr = assembly.GetManifestResourceNames();

            string fullResourceName = $"{assembly.GetName().Name}.Resources.{resourceName}";

            if (!rr.Contains(fullResourceName))
            {
                Debug.WriteLine( "Resource not found - " + fullResourceName);
            }



            return assembly.GetManifestResourceStream(fullResourceName);
        }

        public static string GetString(string resourceName)
        {

            var result = string.Empty;
            try
            {
                using var stream = GetStream(resourceName);

                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    result = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error loading resource - " + e.Message);
            }


            return result;
        }
    }
}
