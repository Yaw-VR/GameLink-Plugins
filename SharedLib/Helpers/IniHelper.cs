using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLib
{
#nullable disable
    internal class IniHelper
    {
        readonly static Regex heading = new Regex(@"\[(.+?)\]");
        readonly static Regex kvp = new Regex(@"(.+?)=(.*)");

        private Dictionary<string, Dictionary<string, string>> sections;

        public static async Task<IniHelper> LoadAsync(string path, CancellationToken cancellationToken = default)
        {

            if (!File.Exists(path))
                throw new FileNotFoundException("File not found", path);

            string ini = await File.ReadAllTextAsync(path, cancellationToken);

            var dict = Parse(ini);

            return new IniHelper(dict);
            
        }

        private static Dictionary<string, Dictionary<string, string>> Parse(string ini)
        {
            var dict = new Dictionary<string, Dictionary<string, string>>();
            string k = null;

            using var sr = new StringReader(ini);

            int i = 0;
            string line = sr.ReadLine();
            while (line != null)
            {
                i++;
                var hm = heading.Match(line);
                if (hm.Success)
                {
                    k = hm.Groups[1].Value.Trim();
                    dict[k] = [];
                    line = sr.ReadLine();
                    continue;
                }

                var m = kvp.Match(line);
                if (m.Success)
                {
                    if (k != null)
                    {
                        dict[k][m.Groups[1].Value] = m.Groups[2].Value?.Trim();
                    }
                    else
                    {
                        throw new Exception(string.Format("Invalid INI file. Line {0} - {1}", i, line));
                    }
                }

                line = sr.ReadLine();
            }

            return dict;
        }

        protected IniHelper(Dictionary<string, Dictionary<string, string>> values)
        {
            this.sections = values;
        }

        public string TryGetValue(string section, string key)
        {
            if (sections.TryGetValue(section, out var dict))
            {
                if (dict.TryGetValue(key, out var value))
                {
                    return value;
                }
            }
            return null;
        }

        public string this[string section, string key]
        {
            get
            {
                return TryGetValue(section, key);
            }
        }

    }
}
