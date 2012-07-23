using System;
using System.Globalization;
using System.IO;
using System.Net;

namespace NuGet
{
    internal sealed class HttpClient
    {
        public void DownloadData(string filePath)
        {
            using (var response = RequestHelper.GetResponse(CreateWebRequest,
                                             (_) => { },
                                             ProxyCache.Instance,
                                             CredentialStore.Instance,
                                             new ConsoleCredentialProvider(new NuGet.Common.Console())))
            {
                using (Stream stream = response.GetResponseStream(),
                              fileStream = File.OpenWrite(filePath))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }

        private static HttpWebRequest CreateWebRequest()
        {
            var httpRequest = (HttpWebRequest)WebRequest.Create("https://nuget.org/nuget.exe");

            httpRequest.UserAgent = String.Format(CultureInfo.InvariantCulture, "NuGet Bootstrapper/{0} ({1})", typeof(HttpClient).Assembly.GetName().Version, Environment.OSVersion);
            httpRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            return httpRequest;
        }
    }
}