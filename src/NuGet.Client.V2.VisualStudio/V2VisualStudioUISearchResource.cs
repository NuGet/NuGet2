using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Client.VisualStudio.Models;
using NuGet.Versioning;

namespace NuGet.Client.V2.VisualStudio
{
    public class V2VisualStudioUISearchResource : V2Resource, IVisualStudioUISearch
    {
        public V2VisualStudioUISearchResource(V2Resource resource)
            : base(resource)
        {
        }

        public Task<IEnumerable<VisualStudioUISearchMetadata>> GetSearchResultsForVisualStudioUI(string searchTerm, SearchFilter filters, int skip, int take, System.Threading.CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                var query = V2Client.Search(
                    searchTerm,
                    filters.SupportedFrameworks,
                    filters.IncludePrerelease,
                    filters.IncludeDelisted);

                // V2 sometimes requires that we also use an OData filter for latest/latest prerelease version
                if (filters.IncludePrerelease)
                {
                    query = query.Where(p => p.IsAbsoluteLatestVersion);
                }
                else
                {
                    query = query.Where(p => p.IsLatestVersion);
                }

                if (V2Client is LocalPackageRepository)
                {
                    // if the repository is a local repo, then query contains all versions of packages.
                    // we need to explicitly select the latest version.
                    query = query.OrderBy(p => p.Id)
                        .ThenByDescending(p => p.Version)
                        .GroupBy(p => p.Id)
                        .Select(g => g.First());
                }

                // Now apply skip and take and the rest of the party
                return (IEnumerable<VisualStudioUISearchMetadata>)query
                    .Skip(skip)
                    .Take(take)
                    .ToList()
                    .AsParallel()
                    .AsOrdered()
                    .Select(p => CreatePackageSearchResult(p, cancellationToken))
                    .ToList();
            }, cancellationToken);
        }

        private VisualStudioUISearchMetadata CreatePackageSearchResult(IPackage package, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var versions = V2Client.FindPackagesById(package.Id);
            if (!versions.Any())
            {
                versions = new[] { package };
            }
            string id = package.Id;
            NuGetVersion version = SafeToNuGetVer(package.Version);
            string summary = package.Summary;
            IEnumerable<NuGetVersion> nuGetVersions = versions.Select(p => SafeToNuGetVer(p.Version));
            if (string.IsNullOrWhiteSpace(summary))
            {
                summary = package.Description;
            }

            Uri iconUrl = package.IconUrl;
            VisualStudioUISearchMetadata searchMetaData = new VisualStudioUISearchMetadata(id, version, summary, iconUrl, nuGetVersions, null);
            return searchMetaData;
        }

        private static NuGetVersion SafeToNuGetVer(SemanticVersion semanticVersion)
        {
            if (semanticVersion == null)
            {
                return null;
            }
            return new NuGetVersion(
                semanticVersion.Version,
                semanticVersion.SpecialVersion);
        }
    }
}