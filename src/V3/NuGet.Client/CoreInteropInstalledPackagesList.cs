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

        public override IEnumerable<InstalledPackageReference> GetInstalledPackageReferences()
        {
            return _localRepository.GetPackages()
                .SelectMany(p => _localRepository.GetPackageReferences(p.Id))
                .Select(pr => CoreConverters.SafeToInstalledPackageReference(pr));
        }

        public override InstalledPackageReference GetInstalledPackage(string packageId)
        {
            NuGetTraceSources.ProjectInstalledPackagesList.Verbose("getver", "Getting installed version of {0}", packageId);
            return CoreConverters.SafeToInstalledPackageReference(_localRepository.GetPackageReference(packageId));
        }

        public override bool IsInstalled(string packageId, NuGetVersion packageVersion)
        {
            NuGetTraceSources.ProjectInstalledPackagesList.Verbose("isinstalled", "IsInstalled? {0} {1}", packageId, packageVersion.ToNormalizedString());
            return _localRepository.Exists(
                packageId,
                new SemanticVersion(packageVersion.Version, packageVersion.Release));
        }

        public override Task<IEnumerable<JObject>> Search(string searchTerm, int skip, int take, CancellationToken cancelToken)
        {
            NuGetTraceSources.ProjectInstalledPackagesList.Verbose("search", "Search: {0}", searchTerm);
            return Task.FromResult(
                _localRepository.Search(searchTerm, allowPrereleaseVersions: true)
                    .Skip(skip).Take(take).ToList()
                    .Select(p => PackageJsonLd.CreatePackageSearchResult(p, new[] { p })));
        }

        public override Task<IEnumerable<JObject>> GetAllInstalledPackagesAndMetadata()
        {
            NuGetTraceSources.ProjectInstalledPackagesList.Verbose("getallmetadata", "Get all installed packages and metadata");
            return Task.FromResult(
                _localRepository
                    .GetPackages().ToList()
                    .Select(p => PackageJsonLd.CreatePackage(p)));
        }
    }
}
