using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Client.Installation;
using NuGet.Client.ProjectSystem;
using NuGet.VisualStudio;
using DteSolution = EnvDTE.Solution;
using DteProject = EnvDTE.Project;
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Client.Interop;

namespace NuGet.Client.VisualStudio
{
    public class VsSolution : Solution
    {
        private readonly List<VsProject> _projects;
        private readonly string _name;
        private readonly InstalledPackagesList _installedSolutionLevelPackages;
        private readonly IVsPackageManager _packageManager;
        
        public override bool IsAvailable
        {
            get
            {
                return DteSolution.IsOpen;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override IReadOnlyList<Project> Projects
        {
            get
            {
                return _projects.AsReadOnly();
            }
        }

        public DteSolution DteSolution { get; set; }

        public override InstalledPackagesList InstalledPackages
        {
            get
            {
                return _installedSolutionLevelPackages;
            }
        }

        public VsSolution(DteSolution dteSolution, ISolutionManager solutionManager, IVsPackageManager packageManager)
        {
            _name = String.Format(
                CultureInfo.CurrentCulture,
                Strings.Label_Solution,
                dteSolution.GetName());
            _projects = LoadProjects(solutionManager, packageManager).ToList();
            _packageManager = packageManager;

            var repo = (SharedPackageRepository)packageManager.LocalRepository;
            _installedSolutionLevelPackages = new CoreInteropInstalledPackagesList(
                new PackageReferenceRepository(repo.PackageReferenceFile.FullPath, repo));

            DteSolution = dteSolution;

            // Add V2-related interop features
            AddFeature(() => packageManager.LocalRepository);
            AddFeature<IPackageManager>(() => packageManager);
            AddFeature<IPackageCacheRepository>(() => MachineCache.Default);

            // Add PowerShell feature
            AddFeature<PowerShellScriptExecutor>(() =>
                new VsPowerShellScriptExecutor(ServiceLocator.GetInstance<IScriptExecutor>()));
        }

        public override bool Equals(Solution other)
        {
            VsSolution vsSln = other as VsSolution;
            return vsSln != null &&
                String.Equals(vsSln.DteSolution.FileName, DteSolution.FileName, StringComparison.OrdinalIgnoreCase);
        }

        public VsProject GetProject(DteProject dteProject)
        {
            return _projects.FirstOrDefault(
                p => String.Equals(p.DteProject.UniqueName, dteProject.UniqueName, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<VsProject> LoadProjects(ISolutionManager solutionManager, IVsPackageManager packageManager)
        {
            return solutionManager.GetProjects().Select(dteProject =>
                new VsProject(this, dteProject, packageManager.GetProjectManager(dteProject)));
        }

        public override Task<IEnumerable<JObject>> SearchInstalled(string searchText, int skip, int take, CancellationToken ct)
        {
            return Task.FromResult(
                _packageManager.LocalRepository.Search(searchText, allowPrereleaseVersions: true)
                    .Skip(skip)
                    .Take(take)
                    .ToList()
                    .Select(p => PackageJsonLd.CreatePackageSearchResult(p, new[] { p }, hasAdditionalVersions: false)));
        }
    }
}