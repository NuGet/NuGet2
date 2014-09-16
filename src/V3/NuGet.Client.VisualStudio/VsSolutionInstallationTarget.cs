using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Interop;
using NuGet.VisualStudio;
using System.Linq;

namespace NuGet.Client.VisualStudio
{
    public class VsSolutionInstallationTarget : InstallationTarget
    {
        private EnvDTE.Solution _solution;
        private string _name;
        private IVsPackageManager _packageManager;
        private IPackageRepository _packagesFolderSource;
        private InstalledPackagesList _installedSolutionLevelPackages;

        public override bool IsActive
        {
            get
            {
                return _solution.IsOpen;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override IEnumerable<InstalledPackagesList> InstalledPackagesInAllProjects
        {
            get {
                return _solution.GetAllProjects()
                    .Select(p => (InstalledPackagesList)new ProjectInstalledPackagesList(
                        (PackageReferenceRepository)_packageManager.GetProjectManager(p).LocalRepository));
            }
        }

        public InstalledPackagesList InstalledSolutionLevelPackages
        {
            get
            {
                return _installedSolutionLevelPackages;
            }
        }

        public override IEnumerable<TargetProject> TargetProjects
        {
            get {
                return _solution.GetAllProjects()
                    .Select(p => new VsTargetProject(p, _packageManager.GetProjectManager(p)));
            }
        }

        public VsSolutionInstallationTarget(EnvDTE.Solution solution)
        {
            _solution = solution;
            _name = string.Format(
                CultureInfo.CurrentCulture,
                Strings.Label_Solution,
                _solution.GetName());

            _packageManager = ServiceLocator.GetInstance<IVsPackageManagerFactory>()
                .CreatePackageManagerToManageInstalledPackages();
            _packagesFolderSource = _packageManager.LocalRepository;

            var repo = _packageManager.LocalRepository as SharedPackageRepository;
            var refRepo = new PackageReferenceRepository(repo.PackageReferenceFile.FullPath, _packageManager.LocalRepository);
            _installedSolutionLevelPackages = new ProjectInstalledPackagesList(refRepo);
        }

        public override Task<IEnumerable<JObject>> SearchInstalled(string searchText, int skip, int take, CancellationToken ct)
        {
            return Task.FromResult(
                _packagesFolderSource.Search(searchText, allowPrereleaseVersions: true)
                    .Skip(skip)
                    .Take(take)
                    .ToList()
                    .Select(p => PackageJsonLd.CreatePackageSearchResult(p, new[] { p })));
        }
    }
}