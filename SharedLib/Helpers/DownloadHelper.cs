using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#nullable disable
namespace SharedLib
{

    internal class DownloadHelper
    {
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3";
        public static async Task<string> DownloadFileAsync(string url, string folder = null,  CancellationToken cancellationToken = default)
        {

           
            if (folder == null)
            {
                folder = Path.GetTempPath();
            }
            else if(!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            

            
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                var response = await httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    //var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                    
                    var filename = response.Content.Headers?.ContentDisposition?.FileName ?? Path.GetFileName(Path.GetTempFileName());
                    
                    
                    var fullPath = Path.Combine(folder, filename);

                    using FileStream fileStream = File.Create(fullPath);

                    using var s = await response.Content.ReadAsStreamAsync(cancellationToken);
                    s.Seek(0, SeekOrigin.Begin);
                    await s.CopyToAsync(fileStream, cancellationToken);

                    
                    //await File.WriteAllBytesAsync(fullPath, fileBytes, cancellationToken);

                    Console.WriteLine($@"Downloaded {url} to ""{fullPath}"" ");//({fileBytes.Length / 1024}) kb

                    return fullPath;
                }
                else
                {
                    throw new Exception("Error downloading file: " + response.ReasonPhrase);

                }
            }
            catch (Exception e)
            {

            }

            return null;
        }
        
    }

    


}
