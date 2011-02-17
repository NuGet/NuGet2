using System;
using System.ComponentModel.Composition;
using EnvDTE;
using Microsoft.Internal.Web.Utils;

namespace NuGet.VisualStudio {
    [Export(typeof(IVsPackageInstaller))]
    public class VsPackageInstaller : IVsPackageInstaller {
        private readonly IVsPackageManagerFactory _packageManagerFactory;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly IPackageRepositoryFactory _repositoryFactory;

        [ImportingConstructor]
        public VsPackageInstaller(IVsPackageManagerFactory packageManagerFactory, 
                                  IScriptExecutor scriptExecutor,
                                  IPackageRepositoryFactory repositoryFactory) {
            _packageManagerFactory = packageManagerFactory;
            _scriptExecutor = scriptExecutor;
            _repositoryFactory = repositoryFactory;
        }

        public void InstallPackage(string source, Project project, string packageId, Version version, bool ignoreDependencies) {
            if (String.IsNullOrEmpty(source)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "source");
            }

            IPackageRepository repository = _repositoryFactory.CreateRepository(new PackageSource(source));
            InstallPackage(repository, project, packageId, version, ignoreDependencies);            
        }

        internal void InstallPackage(IPackageRepository repository, Project project, string packageId, Version version, bool ignoreDependencies) {
            if (project == null) {
                throw new ArgumentNullException("project");
            }

            IVsPackageManager packageManager = _packageManagerFactory.CreatePackageManager(repository);
            IProjectManager projectManager = packageManager.GetProjectManager(project);

            EventHandler<PackageOperationEventArgs> installedHandler = (sender, e) => {
                _scriptExecutor.ExecuteInitScript(e.InstallPath, e.Package, NullLogger.Instance);
            };

            EventHandler<PackageOperationEventArgs> addedHandler = (sender, e) => {
                _scriptExecutor.ExecuteScript(e.InstallPath, PowerShellScripts.Install, e.Package, project, NullLogger.Instance);
            };

            try {
                projectManager.PackageReferenceAdded += addedHandler;
                packageManager.PackageInstalled += installedHandler;

                packageManager.InstallPackage(projectManager, packageId, version, ignoreDependencies, NullLogger.Instance);
            }
            finally {
                projectManager.PackageReferenceAdded -= addedHandler;
                packageManager.PackageInstalled -= installedHandler;
            }
        }
    }
}
