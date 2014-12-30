using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V2
{
    public static class V2Utilities
    {
        public async static Task<bool> IsV2(PackageSource source)
        {
            var url = new Uri(source.Url);
            if (Directory.Exists(url.LocalPath) || url.IsUnc) // Check if a local path or a UNC share is specified. For Local path sources, we will continue to create V2 resources for now.
            {
                return true;
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

        public static IPackageRepository GetV2SourceRepository(PackageSource source, string host)
        {           
            IPackageRepository repo = new PackageRepositoryFactory().CreateRepository(source.Url);
            LocalPackageRepository _lprepo = repo as LocalPackageRepository;
            if (_lprepo != null)
                return _lprepo;
            string _userAgent = UserAgentUtil.GetUserAgent("NuGet.Client.Interop", host);
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
