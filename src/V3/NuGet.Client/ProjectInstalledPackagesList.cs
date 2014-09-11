using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Interop;
using NuGet.Versioning;

namespace NuGet.Client
{
    public class ProjectInstalledPackagesList : InstalledPackagesList
    {
        private IPackageRepository _localRepository;

        public ProjectInstalledPackagesList(IPackageRepository localRepository)
        {
            _localRepository = localRepository;
        }

        public override IEnumerable<PackageIdentity> GetAllPackages()
        {
            return _localRepository.GetPackages().Select(p => new PackageIdentity(
                p.Id,
                new NuGetVersion(p.Version.Version, p.Version.SpecialVersion, null)));
        }

        public override NuGetVersion GetInstalledVersion(string packageId)
        {
            var package = _localRepository.FindPackage(packageId);
            if (package == null)
            {
                return null;
            }
            return new NuGetVersion(package.Version.Version, package.Version.SpecialVersion);
        }

        public override bool IsInstalled(string packageId, NuGetVersion packageVersion)
        {
            return _localRepository.Exists(
                packageId,
                new SemanticVersion(packageVersion.Version, packageVersion.Release));
        }

        public override Task<IEnumerable<JObject>> Search(string searchTerm, int skip, int take, CancellationToken cancelToken)
        {
            return Task.FromResult(
                _localRepository.Search(searchTerm, allowPrereleaseVersions: true)
                    .Skip(skip).Take(take).ToList()
                    .Select(p => PackageJsonLd.CreatePackageSearchResult(p, new[] { p })));
        }

        public override Task<IEnumerable<JObject>> GetAllInstalledPackagesAndMetadata()
        {
            return Task.FromResult(
                _localRepository
                    .GetPackages().ToList()
                    .Select(p => PackageJsonLd.CreatePackage(p)));
        }
    }
}
