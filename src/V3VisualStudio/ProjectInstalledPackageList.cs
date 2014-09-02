using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using NuGet.Client.Interop;
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

        public IEnumerable<PackageName> GetInstalledPackages()
        {
            return _projectManager.LocalRepository.GetPackages().Select(p => new PackageName(p.Id, p.Version));
        }

        public SemanticVersion GetInstalledVersion(string packageId)
        {
            var package = _projectManager.LocalRepository.FindPackage(packageId);
            if (package == null)
            {
                return null;
            }
            return package.Version;
        }

        public bool IsInstalled(string packageId, SemanticVersion packageVersion)
        {
            return _projectManager.LocalRepository.Exists(packageId, packageVersion);
        }

        public IPackageSearcher CreateSearcher()
        {
            return new V2InteropSearcher(_projectManager.LocalRepository);
        }
    }
}
