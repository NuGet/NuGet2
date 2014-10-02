using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Installation;
using NuGet.VisualStudio;
using NuGet.Client.ProjectSystem;
using DteProject = EnvDTE.Project;
using System;
using System.Runtime.Versioning;

namespace NuGet.Client.VisualStudio
{
    public class VsProject : Project
    {
        private readonly InstalledPackagesList _installed;
        private readonly VsSolution _solution;

        public DteProject DteProject { get; private set; }

        public override string Name
        {
            get { return DteProject.Name; }
        }

        public override bool IsAvailable
        {
            get { return !DteProject.IsUnloaded(); }
        }

        public override InstalledPackagesList InstalledPackages
        {
            get
            {
                return _installed;
            }
        }

        public VsProject(VsSolution solution, DteProject dteProject, IProjectManager projectManager)
            : base()
        {
            _solution = solution;
            _installed = new CoreInteropInstalledPackagesList((IPackageReferenceRepository2)projectManager.LocalRepository);
            DteProject = dteProject;
            
            // Add V2-related interop features
            AddFeature(() => projectManager);
            AddFeature(() => projectManager.PackageManager);
            AddFeature(() => projectManager.Project);
            AddFeature(() => projectManager.PackageManager.LocalRepository);
            AddFeature<IPackageCacheRepository>(() => MachineCache.Default);

            // Add PowerShell feature
            AddFeature<PowerShellScriptExecutionFeature>(() =>
                new VsPowerShellScriptExecutionFeature(ServiceLocator.GetInstance<IScriptExecutor>()));
        }

        public static VsProject Create(VsSolution solution, DteProject dteProject)
        {
            VsNuGetTraceSources.VsProjectInstallationTarget.Verbose("create", "Created install target for project: {0}", dteProject.Name);
            var projectManager = ServiceLocator.GetInstance<IVsPackageManagerFactory>()
                .CreatePackageManagerToManageInstalledPackages()
                .GetProjectManager(dteProject);
            return new VsProject(solution, dteProject, projectManager);
        }

        public override bool Equals(Project other)
        {
            VsProject vsProj = other as VsProject;
            return vsProj != null && String.Equals(vsProj.DteProject.FileName, DteProject.FileName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Project);
        }

        public override int GetHashCode()
        {
            return DteProject.FileName.ToLowerInvariant().GetHashCode();
        }

        public override Solution GetSolution()
        {
            return _solution;
        }

        public override FrameworkName GetSupportedFramework()
        {
            return DteProject.GetTargetFrameworkName();
        }

        public override Task<IEnumerable<JObject>> SearchInstalled(string searchText, int skip, int take, CancellationToken cancelToken)
        {
            return InstalledPackages.Search(searchText, skip, take, cancelToken);
        }
    }
}
