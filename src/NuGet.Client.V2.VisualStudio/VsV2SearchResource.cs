using NuGet.Client;
using NuGet.Client.V2;
using NuGet.Client.VisualStudio.Models;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace NuGet.Client.V2.VisualStudio
{
    [Export(typeof(V2Resource))]
    public class VsV2SearchResource : V2Resource, IVsSearch
    {
      public VsV2SearchResource():base(null,null)
        {

        }
                     
        public VsV2SearchResource(IPackageRepository repo,string host):base(repo,host)
        {
                    
        }

        public Task<IEnumerable<VisualStudioUISearchMetaData>> GetSearchResultsForVisualStudioUI(string searchTerm, SearchFilter filters, int skip, int take, System.Threading.CancellationToken cancellationToken)
        {
  
            return Task.Factory.StartNew(() =>
            {
                var query = V2Client.Search(
                    searchTerm,
                    filters.SupportedFrameworks.Select(fx => fx.FullName),
                    filters.IncludePrerelease);

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
                return (IEnumerable<VisualStudioUISearchMetaData>)query
                    .Skip(skip)
                    .Take(take)
                    .ToList()
                    .AsParallel()
                    .AsOrdered()
                    .Select(p => CreatePackageSearchResult(p, cancellationToken))
                    .ToList();
            }, cancellationToken);
        }     
        private VisualStudioUISearchMetaData CreatePackageSearchResult(IPackage package, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();         
            var versions = V2Client.FindPackagesById(package.Id);
            if (!versions.Any())
            {
                versions = new[] { package };
            }

            VisualStudioUISearchMetaData searchMetaData = new VisualStudioUISearchMetaData();
            searchMetaData.Id = package.Id;
            searchMetaData.Version = SafeToNuGetVer(package.Version);
            searchMetaData.Summary = package.Summary;
            searchMetaData.Versions = versions.Select(p => SafeToNuGetVer(p.Version));
            if (string.IsNullOrWhiteSpace(package.Summary))
                searchMetaData.Summary = package.Summary;
            else
                searchMetaData.Summary = package.Description;
            searchMetaData.IconUrl = package.IconUrl;
            return searchMetaData;
        }
        
        public override string Description
        {
            get { throw new NotImplementedException(); }
        }
        private NuGetVersion SafeToNuGetVer(SemanticVersion semanticVersion)
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
