using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Newtonsoft.Json.Linq;
using NuGet.Client.Interop;
using NuGet.Versioning;
using NuGet.VisualStudio;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client.VisualStudio
{
    public class VsProjectInstallationTarget : InstallationTarget
    {
        private readonly IProjectManager _projectManager;

        public Project Project { get; private set; }

        public override string Name
        {
            get { return Project.Name; }
        }

        public VsProjectInstallationTarget(Project project, IProjectManager projectManager)
        {
            Project = project;
            _projectManager = projectManager;
        }

        public static VsProjectInstallationTarget Create(Project project)
        {
            var projectManager = ServiceLocator.GetInstance<IVsPackageManagerFactory>()
                .CreatePackageManagerToManageInstalledPackages()
                .GetProjectManager(project);
            return new VsProjectInstallationTarget(project, projectManager);
        }

        public override IEnumerable<PackageIdentity> GetInstalledPackages()
        {
            return _projectManager.LocalRepository.GetPackages().Select(p => new PackageIdentity(
                p.Id,
                new NuGetVersion(p.Version.Version, p.Version.SpecialVersion, null)));
        }

        public override NuGetVersion GetInstalledVersion(string packageId)
        {
            var package = _projectManager.LocalRepository.FindPackage(packageId);
            if (package == null)
            {
                return null;
            }
            return new NuGetVersion(package.Version.Version, package.Version.SpecialVersion);
        }

        public override bool IsInstalled(string packageId, NuGetVersion packageVersion)
        {
            return _projectManager.LocalRepository.Exists(
                packageId,
                new SemanticVersion(packageVersion.Version, packageVersion.Release));
        }

        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            yield return Project.GetTargetFrameworkName();
        }

        public override Task<IEnumerable<JObject>> SearchInstalledPackages(string searchTerm, int skip, int take, CancellationToken cancelToken)
        {
            return Task.FromResult(
                _projectManager.LocalRepository.Search(searchTerm, allowPrereleaseVersions: true)
                    .Skip(skip).Take(take).ToList()
                    .Select(p => PackageJsonLd.CreatePackageSearchResult(p, new[] { p })));
        }

        public override Task ExecuteActionsAsync(IEnumerable<NewPackageAction> actions)
        {
            // No-op temporarily
            return Task.FromResult(0);
            //throw new NotImplementedException();
        }
    }
}
