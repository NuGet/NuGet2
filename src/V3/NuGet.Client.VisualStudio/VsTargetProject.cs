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
using NuGet.Client.Resolution;
using NuGet.Resolver;
using NuGet.Versioning;
using NuGet.VisualStudio;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client.VisualStudio
{
    public class VsTargetProject : TargetProject
    {
        private readonly InstalledPackagesList _installedPackages;

        public Project Project { get; private set; }
        public IProjectManager ProjectManager { get; private set; }
        
        public override string Name
        {
            get { return Project.Name; }
        }

        public override IProjectSystem ProjectSystem
        {
            get { return ProjectManager.Project; }
        }

        public override InstalledPackagesList InstalledPackages
        {
            get { return _installedPackages; }
        }

        public VsTargetProject(Project project, IProjectManager projectManager)
            : this(project, projectManager, (IPackageReferenceRepository2)projectManager.LocalRepository)
        {
        }

        public VsTargetProject(Project project, IProjectManager projectManager, IPackageReferenceRepository2 localRepository)
        {
            Project = project;
            ProjectManager = projectManager;
            _installedPackages = new CoreInteropInstalledPackagesList(localRepository);
        }

        public override FrameworkName GetSupportedFramework()
        {
            return Project.GetTargetFrameworkName();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TargetProject);
        }

        public override bool Equals(TargetProject other)
        {
            var vsProj = other as VsTargetProject;
            return vsProj != null &&
                String.Equals(vsProj.Project.UniqueName, Project.UniqueName, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Project.UniqueName.ToLowerInvariant().GetHashCode();
        }
    }
}
