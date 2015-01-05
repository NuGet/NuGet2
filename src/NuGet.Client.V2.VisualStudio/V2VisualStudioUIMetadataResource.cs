using NuGet.Client;
using NuGet.Client.V2;
using NuGet.Client.VisualStudio.Models;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace NuGet.Client.V2.VisualStudio
{
    
    public class V2VisualStudioUIMetadataResource : V2Resource, IVisualStudioUIMetadata
    {
        public V2VisualStudioUIMetadataResource(V2Resource resource)
            : base(resource)
        {
        }

        public  Task<VisualStudioUIPackageMetadata> GetPackageMetadataForVisualStudioUI(string packageId, NuGetVersion version)
        {
            return Task.Factory.StartNew(() =>
            {              
                var semver = new SemanticVersion(version.ToNormalizedString());
                var package = V2Client.FindPackage(packageId, semver);

                // Sometimes, V2 APIs seem to fail to return a value for Packages(Id=,Version=) requests...
                if (package == null)
                {
                    var packages = V2Client.FindPackagesById(packageId);
                    package = packages.FirstOrDefault(p => Equals(p.Version, semver));
                }

                // If still null, fail
                if (package == null)
                {
                    return null;
                }

                string repoRoot = null;
                IPackagePathResolver resolver = null;
                LocalPackageRepository _lprepo = V2Client as LocalPackageRepository;
                if (_lprepo != null)
                {
                    repoRoot = _lprepo.Source;
                    resolver = _lprepo.PathResolver;
                }

                return GetVisualStudioUIPackageMetadata(package);
            });
        }

        public Task<IEnumerable<VisualStudioUIPackageMetadata>> GetPackageMetadataForAllVersionsForVisualStudioUI(string packageId)
        {
            return Task.Factory.StartNew(() =>
            {
              
                string repoRoot = null;
                IPackagePathResolver resolver = null;
                LocalPackageRepository _lprepo = V2Client as LocalPackageRepository;
                if (_lprepo != null)
                {
                    repoRoot = _lprepo.Source;
                    resolver = _lprepo.PathResolver;
                }
                return V2Client.FindPackagesById(packageId).Select(p => GetVisualStudioUIPackageMetadata(p));
            });
        }

        private static VisualStudioUIPackageMetadata GetVisualStudioUIPackageMetadata(IPackage package)
        {
            NuGetVersion Version = NuGetVersion.Parse(package.Version.ToString());          
            DateTimeOffset? Published = package.Published;
            string Summary = package.Summary;
            string Description = package.Description;
            //*TODOs: Check if " " is the separator in the case of V3 jobjects ...
            string Authors = string.Join(" ",package.Authors.ToArray());
            string Owners = string.Join(" ",package.Owners.ToArray());
            Uri IconUrl = package.IconUrl;
            Uri LicenseUrl = package.LicenseUrl;
            Uri ProjectUrl = package.ProjectUrl;
            string Tags = package.Tags;
            int DownloadCount = package.DownloadCount;
            IEnumerable<VisualStudioUIPackageDependencySet> DependencySets = package.DependencySets.Select(p => GetVisualStudioUIPackageDependencySet(p));

            bool HasDependencies = DependencySets.Any(
                set => set.Dependencies != null && set.Dependencies.Count > 0);

            return new VisualStudioUIPackageMetadata(Version, Summary, Description, Authors, Owners, IconUrl, LicenseUrl, ProjectUrl, Tags, DownloadCount, Published, DependencySets, HasDependencies);
        }

        private static VisualStudioUIPackageDependency GetVisualStudioUIPackageDependency(PackageDependency dependency)
        {
            string id = dependency.Id;
            VersionRange versionRange = dependency.VersionSpec == null ? null : VersionRange.Parse(dependency.VersionSpec.ToString());
            return new VisualStudioUIPackageDependency(id, versionRange);
        }

        private static VisualStudioUIPackageDependencySet GetVisualStudioUIPackageDependencySet(PackageDependencySet dependencySet)
        {
            IEnumerable<VisualStudioUIPackageDependency> visualStudioUIPackageDependencies = dependencySet.Dependencies.Select(d => GetVisualStudioUIPackageDependency(d));
            FrameworkName fxName = dependencySet.TargetFramework;
            return new VisualStudioUIPackageDependencySet(fxName, visualStudioUIPackageDependencies);
        }
      
    }
}
