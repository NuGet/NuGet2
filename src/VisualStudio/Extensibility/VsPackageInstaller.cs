using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using EnvDTE;
using NuGetConsole;

namespace NuGet.VisualStudio
{
    [Export(typeof(IVsPackageInstaller))]
    public class VsPackageInstaller : IVsPackageInstaller
    {
        private readonly IVsPackageManagerFactory _packageManagerFactory;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IOutputConsoleProvider _consoleProvider;
        private readonly IVsCommonOperations _vsCommonOperations;
        private readonly ISolutionManager _solutionManager;
        private readonly IVsWebsiteHandler _websiteHandler;
        private readonly IVsPackageInstallerServices _packageServices;
        private readonly IEnumerable<IRegistryKey> _registryKeys;
        private readonly object _vsExtensionManager;

        [ImportingConstructor]
        public VsPackageInstaller(IVsPackageManagerFactory packageManagerFactory,
                                  IScriptExecutor scriptExecutor,
                                  IPackageRepositoryFactory repositoryFactory,
                                  IOutputConsoleProvider consoleProvider,
                                  IVsCommonOperations vsCommonOperations,
                                  ISolutionManager solutionManager,
                                  IVsWebsiteHandler websiteHandler,
                                  IVsPackageInstallerServices packageServices)
        {
            _packageManagerFactory = packageManagerFactory;
            _scriptExecutor = scriptExecutor;
            _repositoryFactory = repositoryFactory;
            _consoleProvider = consoleProvider;
            _vsCommonOperations = vsCommonOperations;
            _solutionManager = solutionManager;
            _websiteHandler = websiteHandler;
            _packageServices = packageServices;
        }

        /// <summary>
        /// Creates an instance of the package installer for unit testing of registry-based preinstalled packages. This should only be used for unit tests.
        /// </summary>
        /// <param name="registryKeys">The optional list of parent registry keys to look in (used for unit tests).</param>
        internal VsPackageInstaller(IVsPackageManagerFactory packageManagerFactory,
                                    IScriptExecutor scriptExecutor,
                                    IPackageRepositoryFactory repositoryFactory,
                                    IOutputConsoleProvider consoleProvider,
                                    IVsCommonOperations vsCommonOperations,
                                    ISolutionManager solutionManager,
                                    IVsWebsiteHandler websiteHandler,
                                    IVsPackageInstallerServices packageServices,
                                    IEnumerable<IRegistryKey> registryKeys)
            : this(packageManagerFactory, scriptExecutor, repositoryFactory, consoleProvider, vsCommonOperations, solutionManager, websiteHandler, packageServices)
        {
            _registryKeys = registryKeys;
        }

        /// <summary>
        /// Creates an instance of the package installer for unit testing of extension-based preinstalled packages.  This should only be used for unit tests.
        /// </summary>
        /// <param name="vsExtensionManager">A mock extension manager instance (used for unit tests).</param>
        internal VsPackageInstaller(IVsPackageManagerFactory packageManagerFactory,
                                    IScriptExecutor scriptExecutor,
                                    IPackageRepositoryFactory repositoryFactory,
                                    IOutputConsoleProvider consoleProvider,
                                    IVsCommonOperations vsCommonOperations,
                                    ISolutionManager solutionManager,
                                    IVsWebsiteHandler websiteHandler,
                                    IVsPackageInstallerServices packageServices,
                                    object vsExtensionManager)
            : this(packageManagerFactory, scriptExecutor, repositoryFactory, consoleProvider, vsCommonOperations, solutionManager, websiteHandler, packageServices)
        {
            _vsExtensionManager = vsExtensionManager;
        }

        [Import]
        public Lazy<IRepositorySettings> RepositorySettings { get; set; }

        public void InstallPackage(string source, Project project, string packageId, Version version, bool ignoreDependencies)
        {
            InstallPackage(source, project, packageId, version == null ? (SemanticVersion)null : new SemanticVersion(version), ignoreDependencies);
        }

        public void InstallPackage(string source, Project project, string packageId, string version, bool ignoreDependencies)
        {
            InstallPackage(source, project, packageId, ToSemanticVersion(version), ignoreDependencies);
        }

        public void InstallPackage(IPackageRepository repository, Project project, string packageId, string version, bool ignoreDependencies, bool skipAssemblyReferences)
        {
            InstallPackage(repository, project, packageId, ToSemanticVersion(version), ignoreDependencies, skipAssemblyReferences: skipAssemblyReferences);
        }

        internal void InstallPackage(string source, Project project, string packageId, SemanticVersion version, bool ignoreDependencies)
        {
            if (String.IsNullOrEmpty(source))
            {
                // if source is null or empty, we fall back to aggregate package source
                source = AggregatePackageSource.Instance.Source;
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

            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            using (_vsCommonOperations.SaveSolutionExplorerNodeStates(_solutionManager))
            {
                IVsPackageManager packageManager = _packageManagerFactory.CreatePackageManager(repository,
                                                                                               useFallbackForDependencies: false);
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
                    // if skipping assembly references, disable binding redirects too.
                    packageManager.BindingRedirectEnabled = !skipAssemblyReferences;
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

        public void InstallPackagesFromRegistryRepository(string keyName, bool isPreUnzipped, bool skipAssemblyReferences, Project project, IDictionary<string, string> packageVersions)
        {
            this.InstallPackagesFromRegistryRepository(keyName, isPreUnzipped, skipAssemblyReferences, ignoreDependencies: true, project: project, packageVersions: packageVersions);
        }

        public void InstallPackagesFromRegistryRepository(string keyName, bool isPreUnzipped, bool skipAssemblyReferences, bool ignoreDependencies, Project project, IDictionary<string, string> packageVersions)
        {
            if (String.IsNullOrEmpty(keyName))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "keyName");
            }

            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            if (packageVersions.IsEmpty())
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageVersions");
            }

            var preinstalledPackageInstaller = new PreinstalledPackageInstaller(_websiteHandler, _packageServices, _vsCommonOperations, _solutionManager);
            var repositoryPath = preinstalledPackageInstaller.GetRegistryRepositoryPath(keyName, _registryKeys, ThrowError);

            var config = GetPreinstalledPackageConfiguration(isPreUnzipped, skipAssemblyReferences, ignoreDependencies, packageVersions, repositoryPath);
            preinstalledPackageInstaller.PerformPackageInstall(this, project, config, RepositorySettings, ShowWarning, ThrowError);
        }

        public void InstallPackagesFromVSExtensionRepository(string extensionId, bool isPreUnzipped, bool skipAssemblyReferences, Project project, IDictionary<string, string> packageVersions)
        {
            InstallPackagesFromVSExtensionRepository(extensionId, isPreUnzipped, skipAssemblyReferences, ignoreDependencies: true, project: project, packageVersions: packageVersions);
        }

        public void InstallPackagesFromVSExtensionRepository(string extensionId, bool isPreUnzipped, bool skipAssemblyReferences, bool ignoreDependencies, Project project, IDictionary<string, string> packageVersions)
        {
            if (String.IsNullOrEmpty(extensionId))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "extensionId");
            }

            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            if (packageVersions.IsEmpty())
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageVersions");
            }

            var preinstalledPackageInstaller = new PreinstalledPackageInstaller(_websiteHandler, _packageServices, _vsCommonOperations, _solutionManager);
            var repositoryPath = preinstalledPackageInstaller.GetExtensionRepositoryPath(extensionId, _vsExtensionManager, ThrowError);

            var config = GetPreinstalledPackageConfiguration(isPreUnzipped, skipAssemblyReferences, ignoreDependencies, packageVersions, repositoryPath);
            preinstalledPackageInstaller.PerformPackageInstall(this, project, config, RepositorySettings, ShowWarning, ThrowError);
        }

        private static PreinstalledPackageConfiguration GetPreinstalledPackageConfiguration(bool isPreUnzipped, bool skipAssemblyReferences, bool ignoreDependencies, IDictionary<string, string> packageVersions, string repositoryPath)
        {
            List<PreinstalledPackageInfo> packageInfos = new List<PreinstalledPackageInfo>();
            foreach (var package in packageVersions)
            {
                packageInfos.Add(new PreinstalledPackageInfo(package.Key, package.Value, skipAssemblyReferences, ignoreDependencies));
            }

            var config = new PreinstalledPackageConfiguration(repositoryPath, packageInfos, isPreUnzipped);
            return config;
        }

        private static void ThrowError(string message)
        {
            throw new InvalidOperationException(message);
        }

        private void ShowWarning(string message)
        {
            IConsole console = _consoleProvider.CreateOutputConsole(requirePowerShellHost: false);
            console.WriteLine(message);
        }

        private static SemanticVersion ToSemanticVersion(string version)
        {
            return version == null ? (SemanticVersion)null : new SemanticVersion(version);
        }
    }
}
