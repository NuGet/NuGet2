using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Client
{
    /// <summary>
    /// Repository factory to create V2 or V3 repo based on the given Url.
    /// TODO : Make the calls IsV2 and IsV3 async.
    /// </summary>
    public class SourceRepositoryFactory
    {
        public static SourceRepository CreateSourceRepository(PackageSource source,string host,IPackageRepositoryFactory repoFactory)
        {
            bool r = IsV3(source);
            if (r)
            {
                return new V3SourceRepository2(source, host);
            }

            r = IsV2(source);
            if (r)
            {
                return new NuGet.Client.Resources.V2SourceRepository2(
                    source, repoFactory.CreateRepository(source.Url), host);

            }

            throw new InvalidOperationException(
                String.Format("source {0} is not available", source.Url));
                       
        }     
       
        private static bool IsV2(PackageSource source)
        {
            var url = new Uri(source.Url);
            if (url.IsFile || url.IsUnc)
            {
                return true;
            }

            using (var client = new Data.DataClient())
            {
                var result = client.GetFile(url);
                if (result == null)
                {
                    return false;
                }

                var raw = result.Result.Value<string>("raw");
                if (raw != null && raw.IndexOf("Packages", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return true;
                }

                return false;
            }
        }

        private static  bool IsV3(PackageSource source)
        {
            var url = new Uri(source.Url);
            if (url.IsFile || url.IsUnc)
            {
                return File.Exists(url.LocalPath);
            }

            using (var client = new Data.DataClient())
            {
                var v3index = client.GetFile(url);
                if (v3index == null)
                {
                    return false;
                }

                var status = v3index.Result.Value<string>("version");
                if (status != null && status.StartsWith("3.0"))
                {
                    return true;
                }

                return false;
            }
        }
    
    }
}
