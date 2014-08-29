using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using NuGet.VisualStudio;
using NuGet.Client;

namespace NuGet.Client.VisualStudio
{
    internal class ProjectPackageManagerSession : VsPackageManagerSession
    {
        private Project _project;
        private IProjectManager _projectManager;

        public override string Name
        {
            get { return _project.Name; }
        }

        public ProjectPackageManagerSession(Project project, IProjectManager projectManager)
        {
            _project = project;
            _projectManager = projectManager;
        }

        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            yield return _project.GetTargetFrameworkName();
        }

        public override IInstalledPackageList GetInstalledPackageList()
        {
            return new ProjectInstalledPackageList(_projectManager);
        }
    }
}
