using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;

namespace NuGet.Client.VisualStudio
{
    [Export(typeof(VsPackageManagerContext))]
    public class VsPackageManagerContext : PackageManagerContext
    {
        private ISolutionManager _solutionManager;
        private Solution _solution;
        private SourceRepositoryManager _sourceManager;
        private IVsPackageManagerFactory _packageManagerFactory;

        public override SourceRepositoryManager SourceManager
        {
            get { return _sourceManager; }
        }

        public override IEnumerable<string> ProjectNames
        {
            get { return _solutionManager.GetProjects().Select(p => p.GetCustomUniqueName()); }
        }

        [ImportingConstructor]
        public VsPackageManagerContext(
            SourceRepositoryManager sourceManager,
            SVsServiceProvider serviceProvider,
            ISolutionManager solutionManager,
            IVsPackageManagerFactory packageManagerFactory)
        {
            _sourceManager = sourceManager;
            _solutionManager = solutionManager;
            _packageManagerFactory = packageManagerFactory;
            _solution = ((_DTE)serviceProvider.GetService(typeof(_DTE))).Solution;
        }

        public override ProjectInstallationTarget CreateProjectInstallationTarget(string projectName)
        {
            var project = _solutionManager.GetProject(projectName);
            return CreateProjectInstallationTarget(project);
        }

        public ProjectInstallationTarget CreateProjectInstallationTarget(Project project)
        {
            return new VsProjectInstallationTarget(
                project,
                _packageManagerFactory.CreatePackageManagerToManageInstalledPackages().GetProjectManager(project));
        }

        public override InstallationTarget CreateSolutionInstallationTarget()
        {
            return new VsSolutionInstallationTarget(
                _solution,
                _packageManagerFactory.CreatePackageManagerToManageInstalledPackages());
        }
    }
}
