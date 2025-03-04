using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
#nullable disable
namespace SharedLib
{
    internal class GithubClient : IDisposable
    {
        private const string _baseUrl = "https://github.com";
        private string _apiUrl;
        private HttpClient _httpClient;
        public HashSet<string> TempFiles { get; private set; }

        //public string UsernameOrOrg { get; }
        //public string Repo { get; }
        public GithubOptions Options { get; }

        public static GithubClient Create(Action<GithubOptions> optionsAction = null)
        {
            var options = new GithubOptions();
            optionsAction?.Invoke(options);

            if (string.IsNullOrWhiteSpace(options.UsernameOrOrganization))
                throw new ArgumentException("UsernameOrOrganization is required");

            if (string.IsNullOrWhiteSpace(options.Repository))
                throw new ArgumentException("Repository is required");

            return new GithubClient(options);
        }

        protected GithubClient(GithubOptions options = null)
        {
            _apiUrl = $"{_baseUrl}/{options.UsernameOrOrganization}/{options.Repository}";
            Options = options ?? new GithubOptions();
            TempFiles = [];
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
            //_httpClient.BaseAddress = new Uri(_apiUrl);
        }

        public async Task<List<GithubAsset>> ListByTagAsync(string tag, string[] filter = null, CancellationToken cancellationToken = default)
        {
            string path = "releases/tag/"+tag;
            //Uri uri = new Uri($"{_apiUrl}/{path}");

            var assets = await ListAssetsAsync(path, filter, cancellationToken);

            return assets;

        }
        public async Task<List<GithubAsset>> ListLatestAsync(string[] filter = null, CancellationToken cancellationToken = default)
        {
            string path = "releases/latest";
            //Uri uri = new Uri($"{_apiUrl}/{path}");

            //return DownloadAssetsAsync(path, filter, cancellationToken);
            var assets = await ListAssetsAsync(path, filter, cancellationToken);
            return assets;
        }

        public async Task<List<GithubAsset>> DownloadByTagAsync(string tag, string[] filter = null, CancellationToken cancellationToken = default)
        {
            //return DownloadAssetsAsync(path, filter, cancellationToken);
            var assets = await ListByTagAsync(tag, filter, cancellationToken);
            return await DownloadAssetsAsync(assets, cancellationToken: cancellationToken);
        }


        public async Task<List<GithubAsset>> DownloadLatestAsync(string[] filter = null, CancellationToken cancellationToken = default)
        {
            //Uri uri = new Uri($"{_apiUrl}/releases/latest");

            //return DownloadAssetsAsync(uri, filter, cancellationToken);
            var assets = await ListLatestAsync(filter, cancellationToken);
            return await DownloadAssetsAsync(assets, cancellationToken: cancellationToken);
        }

        private async Task<List<GithubAsset>> ListAssetsAsync(string path, string[] filter = null, CancellationToken cancellationToken = default)
        {
            filter ??= Array.Empty<string>();

            var assets = new List<GithubAsset>();

            //using var httpClient = new HttpClient();

            var response = await _httpClient.GetAsync(new Uri($"{_apiUrl}/{path}"), cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync(cancellationToken);

                var m = Regex.Match(html, @$"include-fragment.+?src=""{_apiUrl}/(?<assets>releases/expanded_assets/(?<version>.+?))""", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var assetsurl = m.Groups["assets"].Value;
                    string version = m.Groups["version"].Value.Replace(',', '.');

                    response = await _httpClient.GetAsync(new Uri($"{_apiUrl}/{assetsurl}"));

                    if (response.IsSuccessStatusCode)
                    {
                        var html2 = await response.Content.ReadAsStringAsync(cancellationToken);

                        var dlLinks = Regex.Matches(html2, @$"href=""(?<dl>/{Options.UsernameOrOrganization}/{Options.Repository}/releases/download/.+?)""", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                        if (dlLinks.All(_ => _.Success))
                        {
                            assets = dlLinks.Select(_ => new GithubAsset
                            {
                                Name = Path.GetFileName(_.Groups["dl"].Value),
                                Version = version,
                                Location = _baseUrl + _.Groups["dl"].Value
                            }).ToList();

                            if (filter.Any())
                            {
                                assets = assets.Where(r => filter.Any(a => r.Name.Contains(a))).ToList();
                            }

                        }
                    }
                }
            }

            return assets;
        }

        private async Task<List<GithubAsset>> DownloadAssetsAsync(List<GithubAsset> assets, string folder = null, CancellationToken cancellationToken = default)
        {
            folder ??= Path.GetTempPath();
            foreach (var asset in assets)
            {
                var fullPath = Path.Combine(folder, Path.GetFileName(asset.Location));
                if (!Options.CachingEnabled || !File.Exists(fullPath))
                {
                    var tempFile = await DownloadHelper.DownloadFileAsync(asset.Location, folder, cancellationToken);
                    if (tempFile != null)
                    {
                        TempFiles.Add(tempFile);
                        asset.Location = tempFile;
                    }
                    else
                    {
                        Console.WriteLine("Error downloading file " + asset.Location);
                        break;
                    }
                }
                else if (Options.CachingEnabled)
                {
                    asset.Location = fullPath;
                    Console.WriteLine("Using cached file " + fullPath);
                }
            }


            return assets;
        }

        private async Task<List<GithubAsset>> DownloadAssetsByUrlAsync(string uri, string[] filter = null, CancellationToken cancellationToken = default)
        {
            var assets = await ListAssetsAsync(uri, filter, cancellationToken);

            var folder = Path.GetTempPath();

            foreach (var asset in assets)
            {
                var fullPath = Path.Combine(folder, Path.GetFileName(asset.Location));

                if (!Options.CachingEnabled || !File.Exists(fullPath))
                {
                    var tempFile = await DownloadHelper.DownloadFileAsync(asset.Location, folder, cancellationToken);
                    if (tempFile != null)
                    {
                        TempFiles.Add(tempFile);
                        asset.Location = tempFile;
                    }
                    else
                    {
                        Console.WriteLine("Error downloading file " + asset.Location);

                        break;
                    }
                }
                else if (Options.CachingEnabled)
                {
                    Console.WriteLine("Using cached file " + fullPath);
                }
            }


            return assets;
        }

        public void Dispose()
        {
            if (!Options.CachingEnabled)
                CleanTempFiles();
        }

        public void CleanTempFiles()
        {
            var removed = new List<string>();
            

            foreach (var file in TempFiles)
            {
                try
                {
                    File.Delete(file);
                    removed.Add(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error deleting temp file " + file);
                }

                TempFiles.ExceptWith(removed);
            }
        }

    }
    internal class GithubAsset
    {
        /// <summary>
        /// Name of the asset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Version of the asset
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Location of the asset , can be a url or a local file
        /// </summary>
        public string Location { get; set; }

        public Task<string> DownloadTempFileAsync()
        {
            return DownloadHelper.DownloadFileAsync(Location);
        }
    }

    internal class GithubOptions
    {
        public string UsernameOrOrganization { get; set; }
        
        public string Repository { get; set; }

        /// <summary>
        /// if true, temp files will be deleted when the object is disposed
        /// </summary>
        public bool CachingEnabled { get; set; } 

        public string UserAgent { get; set; } = DownloadHelper.UserAgent;
    }

    
}
