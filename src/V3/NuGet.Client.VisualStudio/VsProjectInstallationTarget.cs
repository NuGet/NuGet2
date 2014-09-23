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

        public override bool IsActive
        {
            get { return true; }
        }

        public TargetProject TargetProject { get; private set; }

        public override IEnumerable<InstalledPackagesList> InstalledPackagesInAllProjects
        {
            get
            {
                VsNuGetTraceSources.VsProjectInstallationTarget.Verbose("getinstalledpackages", "Getting all installed packages in all projects");
                return _projectManager.PackageManager.LocalRepository.LoadProjectRepositories()
                        .Select(r => (InstalledPackagesList)new ProjectInstalledPackagesList((PackageReferenceRepository)r));
            }
        }

        public override IEnumerable<TargetProject> TargetProjects
        {
            get
            {
                yield return TargetProject;
            }
        }

        public VsProjectInstallationTarget(Project project, IProjectManager projectManager)
        {
            Project = project;
            _projectManager = projectManager;

            TargetProject = new VsTargetProject(
                Project,
                _projectManager,
                (PackageReferenceRepository)_projectManager.LocalRepository);
        }

        public static VsProjectInstallationTarget Create(Project project)
        {
            VsNuGetTraceSources.VsProjectInstallationTarget.Verbose("create", "Created install target for project: {0}", project.Name);
            var projectManager = ServiceLocator.GetInstance<IVsPackageManagerFactory>()
                .CreatePackageManagerToManageInstalledPackages()
                .GetProjectManager(project);
            return new VsProjectInstallationTarget(project, projectManager);
        }

        public override Task<IEnumerable<JObject>> SearchInstalled(string searchTerm, int skip, int take, CancellationToken cancelToken)
        {
            return TargetProject.InstalledPackages.Search(searchTerm, skip, take, cancelToken);
        }
    }
}
