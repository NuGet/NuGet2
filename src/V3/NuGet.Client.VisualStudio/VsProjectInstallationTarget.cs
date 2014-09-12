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
        private readonly ProjectInstalledPackagesList _installedList;

        public Project Project { get; private set; }

        public override string Name
        {
            get { return Project.Name; }
        }

        public override InstalledPackagesList Installed
        {
            get { return _installedList; }
        }

        public override IProjectSystem ProjectSystem
        {
            get { return _projectManager.Project; }
        }

        public VsProjectInstallationTarget(Project project, IProjectManager projectManager)
        {
            Project = project;
            _projectManager = projectManager;
            _installedList = new ProjectInstalledPackagesList((PackageReferenceRepository)projectManager.LocalRepository);
        }

        public static VsProjectInstallationTarget Create(Project project)
        {
            VsNuGetTraceSources.VsProjectInstallationTarget.Verbose("create", "Created install target for project: {0}", project.Name);
            var projectManager = ServiceLocator.GetInstance<IVsPackageManagerFactory>()
                .CreatePackageManagerToManageInstalledPackages()
                .GetProjectManager(project);
            return new VsProjectInstallationTarget(project, projectManager);
        }

        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            yield return Project.GetTargetFrameworkName();
        }

        public override Task ExecuteActionsAsync(IEnumerable<NewPackageAction> actions)
        {
            // No-op temporarily
            return Task.FromResult(0);
            //throw new NotImplementedException();
        }

        public override Task<IEnumerable<InstalledPackagesList>> GetInstalledPackagesInAllProjects()
        {
            VsNuGetTraceSources.VsProjectInstallationTarget.Verbose("getinstalledpackages", "Getting all installed packages in all projects");
            return Task.FromResult(
                _projectManager.PackageManager.LocalRepository.LoadProjectRepositories()
                    .Select(r => (InstalledPackagesList)new ProjectInstalledPackagesList((PackageReferenceRepository)r)));
        }
    }
}
