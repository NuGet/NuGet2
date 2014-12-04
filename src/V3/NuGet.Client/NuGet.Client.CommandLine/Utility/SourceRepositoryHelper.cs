using NuGet.Client;
using NuGet.Client.Interop;
using System;

namespace NuGet
{
    internal static class SourceRepositoryHelper
    {
        private static readonly PackageSource NuGetV3PreviewSource = new PackageSource(
            "preview.nuget.org",
            "https://az320820.vo.msecnd.net/ver3-preview/index.json");

        const string HostName = "NuGet.CommandLine";
        internal static SourceRepository CreateSourceRepository(IPackageSourceProvider packageSourceProvider)
        {
            // BUGBUG: Hard-coded to always use the first one
            PackageSource firstSource = null;
            var packageSources = packageSourceProvider.LoadPackageSources();
            foreach(var source in packageSources)
            {
                firstSource = source;
            }
            return CreateRepo(firstSource);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "These objects live until end of process, at which point they will be disposed automatically")]
        private static SourceRepository CreateRepo(PackageSource source)
        {
            // For now, be awful. Detect V3 via the source URL itself
            Uri url;
            if (Uri.TryCreate(source.Source, UriKind.RelativeOrAbsolute, out url) &&
                StringComparer.OrdinalIgnoreCase.Equals(NuGetV3PreviewSource.Source, url.ToString()))
            {
                return new V3SourceRepository(new Client.PackageSource(source.Name, source.Source) , HostName);
            }

            return null;
            //return new V2SourceRepository(source, _repoFactory.CreateRepository(source.Url), HostName);
        }
    }
}
