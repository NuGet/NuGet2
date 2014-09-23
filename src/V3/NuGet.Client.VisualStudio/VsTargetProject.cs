using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Newtonsoft.Json.Linq;
using NuGet.Client.Diagnostics;
using NuGet.Client.Interop;
using NuGet.Versioning;
using NuGet.VisualStudio;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;


namespace NuGet.Client.VisualStudio
{
    public class VsTargetProject : TargetProject
    {
        private readonly IProjectManager _projectManager;
        private readonly InstalledPackagesList _installedPackages;

        public Project Project { get; private set; }

        public override string Name
        {
            get { return Project.Name; }
        }

        public override IProjectSystem ProjectSystem
        {
            get { return _projectManager.Project; }
        }

        public override InstalledPackagesList InstalledPackages
        {
            get { return _installedPackages; }
        }

        public VsTargetProject(Project project, IProjectManager projectManager)
            : this(project, projectManager, (PackageReferenceRepository)projectManager.LocalRepository)
        {
        }

        public VsTargetProject(Project project, IProjectManager projectManager, PackageReferenceRepository localRepository)
        {
            Project = project;
            _projectManager = projectManager;
            _installedPackages = new ProjectInstalledPackagesList(localRepository);
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
    }
}
