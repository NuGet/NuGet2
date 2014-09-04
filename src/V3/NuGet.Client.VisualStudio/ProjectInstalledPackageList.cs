using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using NuGet.Client.Interop;
using NuGet.Versioning;
using NuGet.VisualStudio;

namespace NuGet.Client.VisualStudio
{
    internal class ProjectInstalledPackageList : IInstalledPackageList
    {
        private IProjectManager _projectManager;

        public ProjectInstalledPackageList(IProjectManager projectManager)
        {
            _projectManager = projectManager;
        }

        public IEnumerable<PackageIdentity> GetInstalledPackages()
        {
            return _projectManager.LocalRepository.GetPackages().Select(p => new PackageIdentity(
                p.Id, 
                new NuGetVersion(p.Version.Version, p.Version.SpecialVersion, null)));
        }

        public NuGetVersion GetInstalledVersion(string packageId)
        {
            var package = _projectManager.LocalRepository.FindPackage(packageId);
            if (package == null)
            {
                return null;
            }
            return new NuGetVersion(package.Version.Version, package.Version.SpecialVersion);
        }

        public bool IsInstalled(string packageId, NuGetVersion packageVersion)
        {
            return _projectManager.LocalRepository.Exists(
                packageId, 
                new SemanticVersion(packageVersion.Version, packageVersion.Release));
        }

        public IPackageSearcher CreateSearcher()
        {
            return new V2InteropSearcher(_projectManager.LocalRepository);
        }
    }
}
