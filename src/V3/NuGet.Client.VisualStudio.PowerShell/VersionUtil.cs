using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.VisualStudio;
using NuGet.Versioning;
using System.Diagnostics;

namespace NuGet.Client.VisualStudio.PowerShell
{
    public static class VersionUtil
    {
        public static string GetLastestVersionForPackage(SourceRepository repo, string packageId)
        {
            string version = String.Empty;
            try
            {
                Task<IEnumerable<JObject>> packages = repo.GetPackageMetadataById(packageId);
                var r = packages.Result;
                var allVersions = r.Select(p => NuGetVersion.Parse(p.Value<string>(Properties.Version)));
                version = allVersions.OrderByDescending(v => v).FirstOrDefault().ToNormalizedString();
                return version;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("Exception thrown while trying to get the latest version for package {0}: {1}", packageId, ex.Message));
                return version;
            }
        }
    }
}
