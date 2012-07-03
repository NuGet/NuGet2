using System;
using System.ComponentModel.Composition;
using EnvDTE;

namespace NuGet.VisualStudio
{
    [Export(typeof(IVsPackageInstaller))]
    public class VsPackageInstaller : IVsPackageInstaller
    {
        private readonly IVsPackageManagerFactory _packageManagerFactory;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IVsCommonOperations _vsCommonOperations;
        private readonly ISolutionManager _solutionManager;

        [ImportingConstructor]
        public VsPackageInstaller(IVsPackageManagerFactory packageManagerFactory,
                                  IScriptExecutor scriptExecutor,
                                  IPackageRepositoryFactory repositoryFactory,
                                  IVsCommonOperations vsCommonOperations,
                                  ISolutionManager solutionManager)
        {
            _packageManagerFactory = packageManagerFactory;
            _scriptExecutor = scriptExecutor;
            _repositoryFactory = repositoryFactory;
            _vsCommonOperations = vsCommonOperations;
            _solutionManager = solutionManager;
        }

        public void InstallPackage(string source, Project project, string packageId, Version version, bool ignoreDependencies)
        {
            InstallPackage(source, project, packageId, version == null ? (SemanticVersion)null : new SemanticVersion(version), ignoreDependencies);
        }

		public void InstallPackage(string source, Project project, string packageId, string version, bool ignoreDependencies) {
			InstallPackage(source, project, packageId, ToSemanticVersion(version), ignoreDependencies);
		}

		public void InstallPackage(IPackageRepository repository, Project project, string packageId, string version, bool ignoreDependencies, bool skipAssemblyReferences) 
		{
			InstallPackage(repository, project, packageId, ToSemanticVersion(version), ignoreDependencies, skipAssemblyReferences: skipAssemblyReferences);
		}

		internal void InstallPackage(string source, Project project, string packageId, SemanticVersion version, bool ignoreDependencies) {
			if (String.IsNullOrEmpty(source)) {
				throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "source");
			}

			IPackageRepository repository = _repositoryFactory.CreateRepository(source);
			InstallPackage(repository, project, packageId, version, ignoreDependencies, skipAssemblyReferences: false);
		}

        internal void InstallPackage(IPackageRepository repository, Project project, string packageId, SemanticVersion version, bool ignoreDependencies, bool skipAssemblyReferences)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            using (_vsCommonOperations.SaveSolutionExplorerNodeStates(_solutionManager))
            {
                IVsPackageManager packageManager = _packageManagerFactory.CreatePackageManager(repository,
                                                                                               useFallbackForDependencies : false);
                IProjectManager projectManager = packageManager.GetProjectManager(project);

                EventHandler<PackageOperationEventArgs> installedHandler = (sender, e) =>
                                                                               {
                                                                                   _scriptExecutor.ExecuteInitScript(
                                                                                       e.InstallPath, e.Package,
                                                                                       NullLogger.Instance);
                                                                               };

                EventHandler<PackageOperationEventArgs> addedHandler = (sender, e) =>
                                                                           {
                                                                               _scriptExecutor.ExecuteScript(
                                                                                   e.InstallPath,
                                                                                   PowerShellScripts.Install, 
                                                                                   e.Package,
                                                                                   project,
                                                                                   project.GetTargetFrameworkName(),
                                                                                   NullLogger.Instance);
                                                                           };

                bool oldBindingRedirectValue = packageManager.BindingRedirectEnabled;
                try
                {
                    projectManager.PackageReferenceAdded += addedHandler;
                    packageManager.PackageInstalled += installedHandler;
                    packageManager.BindingRedirectEnabled = false;
                    packageManager.InstallPackage(
                        projectManager, 
                        packageId, 
                        version, 
                        ignoreDependencies,
                        allowPrereleaseVersions: true, 
                        skipAssemblyReferences: skipAssemblyReferences, 
                        logger: NullLogger.Instance);
                }
                finally
                {
                    packageManager.BindingRedirectEnabled = oldBindingRedirectValue;
                    projectManager.PackageReferenceAdded -= addedHandler;
                    packageManager.PackageInstalled -= installedHandler;
                }
            }
        }

		private static SemanticVersion ToSemanticVersion(string version) {
			return version == null ? (SemanticVersion)null : new SemanticVersion(version);
		}
    }
}
