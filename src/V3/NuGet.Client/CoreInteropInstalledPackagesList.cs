using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Diagnostics;
using NuGet.Client.Interop;
using NuGet.Versioning;
using NuGet.Client;

namespace NuGet.Client
{
    public class CoreInteropInstalledPackagesList : InstalledPackagesList
    {
        private IPackageReferenceRepository2 _localRepository;

        public CoreInteropInstalledPackagesList(IPackageReferenceRepository2 localRepository)
        {
            _localRepository = localRepository;
        }

        public override IEnumerable<InstalledPackageReference> GetInstalledPackages()
        {
            return _localRepository.GetPackages()
                .SelectMany(p => _localRepository.GetPackageReferences(p.Id))
                .Select(pr => CoreConverters.SafeToInstalledPackageReference(pr));
        }

        public override InstalledPackageReference GetInstalledPackage(string packageId)
        {
            NuGetTraceSources.CoreInteropInstalledPackagesList.Verbose("getver", "Getting installed version of {0}", packageId);
            return CoreConverters.SafeToInstalledPackageReference(_localRepository.GetPackageReference(packageId));
        }

        public override bool IsInstalled(string packageId, NuGetVersion packageVersion)
        {
            NuGetTraceSources.CoreInteropInstalledPackagesList.Verbose("isinstalled", "IsInstalled? {0} {1}", packageId, packageVersion.ToNormalizedString());
            return _localRepository.Exists(
                packageId,
                new SemanticVersion(packageVersion.Version, packageVersion.Release));
        }

        public override bool IsInstalled(string packageId)
        {
            NuGetTraceSources.CoreInteropInstalledPackagesList.Verbose("isinstalled", "IsInstalled? {0}", packageId);
            return _localRepository.Exists(packageId);
        }

        public override async Task<IEnumerable<SearchResult>> Search(SourceRepository source, string searchTerm, int skip, int take, CancellationToken cancelToken)
        {
            NuGetTraceSources.CoreInteropInstalledPackagesList.Verbose("search", "Search: {0}", searchTerm);
            var installedPackages = await Task.Factory.StartNew(() =>
                _localRepository.Search(searchTerm, allowPrereleaseVersions: true)
                    .Skip(skip).Take(take).ToList());

            // start CreatePackageSearchResult() for all packages in parallel
            var createPackageSearchResultTasks = new List<Task<SearchResult>>();
            foreach (var p in installedPackages)
            {
                var task = CreatePackageSearchResult(source, p);
                createPackageSearchResultTasks.Add(task);
            }

            // collect results
            var result = new List<SearchResult>();
            foreach (var task in createPackageSearchResultTasks)
            {
                var searchResult = await task;
                result.Add(searchResult);
            }
            return result;
        }

        private static async Task<SearchResult> CreatePackageSearchResult(SourceRepository source, IPackage package)
        {
            NuGetTraceSources.CoreInteropInstalledPackagesList.Verbose("loading_versions", "Loading versions for {0} from {1}", package.Id, source.Source.Url);

            var versions = new List<SemanticVersion>();
            var packages = await source.GetPackageMetadataById(package.Id);
            foreach (var p in packages)
            {
                var v = SemanticVersion.Parse(p.Value<string>(Properties.Version));
                versions.Add(v);
            }
            SearchResult searchMetaData = new SearchResult();
            searchMetaData.Version = CoreConverters.SafeToNuGetVer(package.Version);
            searchMetaData.Summary = package.Summary;
            //searchMetaData.Versions = versions.Select(p => CoreConverters.SafeToNuGetVer(p));
            if (string.IsNullOrWhiteSpace(package.Summary))
                searchMetaData.Summary = package.Summary;
            else
                searchMetaData.Summary = package.Description;
            //searchMetaData.IconUrl = package.IconUrl;
            return searchMetaData;
        }

        public override Task<IEnumerable<PackageMetadata>> GetAllInstalledPackagesAndMetadata()
        {
            NuGetTraceSources.CoreInteropInstalledPackagesList.Verbose("getallmetadata", "Get all installed packages and metadata");
            return Task.FromResult(
                _localRepository
                    .GetPackages().ToList()
                    .Select(p => CreatePackageMetaData(p)));
        
        }

        private PackageMetadata CreatePackageMetaData(IPackageMetadata package)
        {
            PackageMetadata p = new PackageMetadata();
            p.Id = package.Id;
            p.Summary = package.Summary;
            p.Version = CoreConverters.SafeToNuGetVer(package.Version);
            return p;
        }
    }
}
