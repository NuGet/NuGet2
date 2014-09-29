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
    public class VsProjectInstallationTarget : ProjectInstallationTarget
    {
        public Project Project { get; private set; }

        public override string Name
        {
            get { return Project.Name; }
        }

        public override bool IsActive
        {
            get { return true; }
        }

        public override IEnumerable<InstalledPackagesList> InstalledPackagesInAllProjects
        {
            get
            {
                VsNuGetTraceSources.VsProjectInstallationTarget.Verbose("getinstalledpackages", "Getting all installed packages in all projects");
                return ProjectManager.PackageManager.LocalRepository.LoadProjectRepositories()
                        .Select(r => (InstalledPackagesList)new ProjectInstalledPackagesList((IPackageReferenceRepository2)r));
            }
        }

        public VsProjectInstallationTarget(Project project, IProjectManager projectManager)
            : base(new VsTargetProject(
                project,
                projectManager,
                (IPackageReferenceRepository2)projectManager.LocalRepository))
        {
            Project = project;
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
