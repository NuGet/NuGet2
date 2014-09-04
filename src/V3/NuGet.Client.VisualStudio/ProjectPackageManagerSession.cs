using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using NuGet.VisualStudio;
using NuGet.Client;
using NuGet.Client.Interop;

namespace NuGet.Client.VisualStudio
{
    public class ProjectPackageManagerSession : VsPackageManagerSession
    {
        private Project _project;
        private IProjectManager _projectManager;

        public Project Project { get { return _project; } }

        public override string Name
        {
            get { return _project.Name; }
        }

        public ProjectPackageManagerSession(Project project, IProjectManager projectManager)
            : base(ServiceLocator.GetInstance<ILoggerManager>().GetLogger(typeof(ProjectPackageManagerSession).Name))
        {
            _project = project;
            _projectManager = projectManager;

            Logger.Log(MessageLevel.Debug, "Creating PackageManagerSession for {0}", project.Name);
        }

        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            yield return _project.GetTargetFrameworkName();
        }

        public override IInstalledPackageList GetInstalledPackageList()
        {
            return new ProjectInstalledPackageList(_projectManager);
        }

        public override IActionResolver CreateActionResolver()
        {
            return new V2InteropActionResolver(
                GetActiveRepo(),
                _projectManager,
                ServiceLocator.GetInstance<ILoggerManager>().GetLogger(typeof(V2InteropActionResolver).Name));
        }

        public override IActionExecutor CreateActionExecutor()
        {
            return new V2InteropActionExecutor();
        }
    }
}
