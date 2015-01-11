using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NuGet.Configuration;

namespace NuGet.Client.V2
{
    public static class V2Utilities
    {
        public static async Task<bool> IsV2(Configuration.PackageSource source)
        {
            var url = new Uri(source.Password);

            // If the url is a directory, then it's a V2 source
            if (url.IsFile || url.IsUnc) 
            {
                return !File.Exists(url.LocalPath);
            }

            using (var client = new Data.DataClient())
            {
                var result = await client.GetFile(url);
                if (result == null)
                {
                    return false;
                }

                var raw = result.Value<string>("raw");
                if (raw != null && raw.IndexOf("Packages", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return true;
                }

                return false;
            }
        }

        public static IPackageRepository GetV2SourceRepository(Configuration.PackageSource source)
        {           
            IPackageRepository repo = new PackageRepositoryFactory().CreateRepository(source.Source);
            LocalPackageRepository _lprepo = repo as LocalPackageRepository;
            if (_lprepo != null)
                return _lprepo;
            string _userAgent = UserAgentUtil.GetUserAgent("NuGet.Client.Interop", "host");
            var events = repo as IHttpClientEvents;
            if (events != null)
            {
                events.SendingRequest += (sender, args) =>
                {
                    var httpReq = args.Request as HttpWebRequest;
                    if (httpReq != null)
                    {
                        httpReq.UserAgent = _userAgent;
                    }
                };               
            }
            return repo;
        }

    }
}
