using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.V3;
using System.Runtime.Versioning;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using NuGet.Client.Diagnostics;
using System.Threading;
using NuGet.Client.Interop;

namespace NuGet.Client.Resources
{
    //TODO : Pass host name;
    //TODO : GetUri to common utility
    public class V2SearchResource : SearchResource
    {
        
        private IPackageRepository _repository;
        private string _host;
       
        public V2SearchResource(IPackageRepository repo,string host)
        {  
            _repository = repo;
            _host = host;
        }

        public override Task<IEnumerable<VisualStudioUISearchMetaData>> GetSearchResultsForVisualStudioUI(string searchTerm, SearchFilter filters, int skip, int take, System.Threading.CancellationToken cancellationToken)
        {
  
            return Task.Factory.StartNew(() =>
            {
                var query = _repository.Search(
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

                if (_repository is LocalPackageRepository)
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

        public override Task<IEnumerable<CommandLineSearchResult>> GetSearchResultsForCommandLine(string searchTerm, bool includePrerelease, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<PowershellSearchResult>> GetSearchResultsForPowershellConsole(string searchTerm, SearchFilter filters, int skip, int take, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        private VisualStudioUISearchMetaData CreatePackageSearchResult(IPackage package, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            NuGetTraceSources.V2SourceRepository.Verbose("getallvers", "Retrieving all versions for {0}", package.Id);
            var versions = _repository.FindPackagesById(package.Id);
            if (!versions.Any())
            {
                versions = new[] { package };
            }

            VisualStudioUISearchMetaData searchMetaData = new VisualStudioUISearchMetaData();
            searchMetaData.Id = package.Id;
            searchMetaData.Version = CoreConverters.SafeToNuGetVer(package.Version);
            searchMetaData.Summary = package.Summary;
            searchMetaData.Versions = versions.Select(p => CoreConverters.SafeToNuGetVer(p.Version));
            if (string.IsNullOrWhiteSpace(package.Summary))
                searchMetaData.Summary = package.Summary;
            else
                searchMetaData.Summary = package.Description;
            searchMetaData.IconUrl = package.IconUrl;
            return searchMetaData;
        }
    }
}
