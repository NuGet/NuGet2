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

        public override Task<IEnumerable<JObject>> Search(SourceRepository source, string searchTerm, int skip, int take, CancellationToken cancelToken)
        {
            NuGetTraceSources.CoreInteropInstalledPackagesList.Verbose("search", "Search: {0}", searchTerm);
            return Task.Run(() =>
                _localRepository.Search(searchTerm, allowPrereleaseVersions: true)
                    .Skip(skip).Take(take).ToList()
                    .Select(p => CreatePackageSearchResult(source, p)));
        }

        private static JObject CreatePackageSearchResult(SourceRepository source, IPackage package)
        {
            NuGetTraceSources.CoreInteropInstalledPackagesList.Verbose("loading_versions", "Loading versions for {0} from {1}", package.Id, source.Source.Url);
            var result = PackageJsonLd.CreatePackageSearchResult(package, Enumerable.Empty<IPackage>());
            var versions = new JArray(source.GetPackageMetadataById(package.Id).Result);

            // Need to ensure that the version list has the installed version in it as well
            var currentVersion = versions.FirstOrDefault(t => SemanticVersion.Parse(t["version"].ToString()).Equals(package.Version));
            if(currentVersion == null) {
                currentVersion = PackageJsonLd.CreatePackage(package);
                versions.Add(currentVersion);
            }

            result[Properties.Packages] = versions;
            return result;
        }

        public override Task<IEnumerable<JObject>> GetAllInstalledPackagesAndMetadata()
        {
            NuGetTraceSources.CoreInteropInstalledPackagesList.Verbose("getallmetadata", "Get all installed packages and metadata");
            return Task.FromResult(
                _localRepository
                    .GetPackages().ToList()
                    .Select(p => PackageJsonLd.CreatePackage(p)));
        }
    }
}
