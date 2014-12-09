using Newtonsoft.Json.Linq;
using NuGet.Client;
using NuGet.Client.Interop;
using NuGet.Common;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuGet
{
    // BUGBUG: Need to create a new class called CommandLineSourceRepositoryManager similar to the factory class used by VS
    internal static class SourceRepositoryHelper
    {
        private static readonly PackageSource NuGetV3PreviewSource = new PackageSource(
            "https://az320820.vo.msecnd.net/ver3-preview/index.json",
            "preview.nuget.org");

        internal static SourceRepository CreateSourceRepository(IPackageSourceProvider packageSourceProvider, IEnumerable<string> sources)
        {
            PackageSource firstSource = null;
            if (sources != null && sources.Any())
            {
                // BUGBUG: Hard-coded to only use the first one
                string firstSourceString = sources.FirstOrDefault();
                firstSource = String.IsNullOrEmpty(firstSourceString) ? null : new PackageSource(firstSourceString);
            }
            else
            {
                // BUGBUG: Hard-coded to only use the first one
                firstSource = packageSourceProvider.LoadPackageSources().FirstOrDefault();                
            }

            return firstSource != null ? CreateRepo(firstSource) : null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "These objects live until end of process, at which point they will be disposed automatically")]
        private static SourceRepository CreateRepo(PackageSource source)
        {
            // For now, be awful. Detect V3 via the source URL itself
            Uri url;
            if (Uri.TryCreate(source.Source, UriKind.RelativeOrAbsolute, out url) &&
                StringComparer.OrdinalIgnoreCase.Equals(NuGetV3PreviewSource.Source, url.ToString()))
            {
                return new V3SourceRepository(new Client.PackageSource(source.Name, source.Source) , CommandLineConstants.UserAgent);
            }

            return null;
            //return new V2SourceRepository(source, _repoFactory.CreateRepository(source.Url), HostName);
        }

        // BUGBUG: Beware! Do not start piling up on this class to add more extension like methods
        // This is temporary until we get a dedicated endpoint to get the latest version
        internal static async Task<JObject> GetLatestVersionMetadata(SourceRepository sourceRepository, string packageId, bool prerelease)
        {
            var packages = await sourceRepository.GetPackageMetadataById(packageId);
            JObject latestPackage = null;
            NuGetVersion latestPackageVersion = null;
            foreach(var package in packages)
            {
                var packageVersion = NuGetVersion.Parse(package["version"].ToString());
                if (!prerelease && packageVersion.IsPrerelease)
                    continue;

                if(latestPackageVersion == null || latestPackageVersion < packageVersion)
                {
                    latestPackageVersion = packageVersion;
                    latestPackage = package;
                }
            }

            return latestPackage;
        }
    }
}
