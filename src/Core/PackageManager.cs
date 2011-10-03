using System;
using System.Globalization;
using System.Linq;
using Microsoft.Internal.Web.Utils;
using NuGet.Resources;

namespace NuGet {
    public class PackageManager : IPackageManager {
        private ILogger _logger;
        private readonly IPackageRepository _cacheRepository;

        private event EventHandler<PackageOperationEventArgs> _packageInstalling;
        private event EventHandler<PackageOperationEventArgs> _packageInstalled;
        private event EventHandler<PackageOperationEventArgs> _packageUninstalling;
        private event EventHandler<PackageOperationEventArgs> _packageUninstalled;

        public PackageManager(IPackageRepository sourceRepository, string path)
            : this(sourceRepository, new DefaultPackagePathResolver(path), new PhysicalFileSystem(path)) {
        }

        public PackageManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, IFileSystem fileSystem) :
            this(sourceRepository, pathResolver, fileSystem, new LocalPackageRepository(pathResolver, fileSystem)) {
        }

        public PackageManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, IFileSystem fileSystem, IPackageRepository localRepository) :
            this(sourceRepository, pathResolver, fileSystem, localRepository, MachineCache.Default) {
        }

        public PackageManager(
            IPackageRepository sourceRepository,
            IPackagePathResolver pathResolver,
            IFileSystem fileSystem,
            IPackageRepository localRepository,
            IPackageRepository cacheRepository) {
            if (sourceRepository == null) {
                throw new ArgumentNullException("sourceRepository");
            }
            if (pathResolver == null) {
                throw new ArgumentNullException("pathResolver");
            }
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            if (localRepository == null) {
                throw new ArgumentNullException("localRepository");
            }
            if (cacheRepository == null) {
                throw new ArgumentNullException("cacheRepository");
            }

            SourceRepository = sourceRepository;
            PathResolver = pathResolver;
            FileSystem = fileSystem;
            LocalRepository = localRepository;
            _cacheRepository = cacheRepository;
        }

        public event EventHandler<PackageOperationEventArgs> PackageInstalled {
            add {
                _packageInstalled += value;
            }
            remove {
                _packageInstalled -= value;
            }
        }

        public event EventHandler<PackageOperationEventArgs> PackageInstalling {
            add {
                _packageInstalling += value;
            }
            remove {
                _packageInstalling -= value;
            }
        }

        public event EventHandler<PackageOperationEventArgs> PackageUninstalling {
            add {
                _packageUninstalling += value;
            }
            remove {
                _packageUninstalling -= value;
            }
        }

        public event EventHandler<PackageOperationEventArgs> PackageUninstalled {
            add {
                _packageUninstalled += value;
            }
            remove {
                _packageUninstalled -= value;
            }
        }

        public IFileSystem FileSystem {
            get;
            set;
        }

        public IPackageRepository SourceRepository {
            get;
            private set;
        }

        public IPackageRepository LocalRepository {
            get;
            private set;
        }

        public IPackagePathResolver PathResolver {
            get;
            private set;
        }

        public ILogger Logger {
            get {
                return _logger ?? NullLogger.Instance;
            }
            set {
                _logger = value;
            }
        }

        public void InstallPackage(string packageId) {
            InstallPackage(packageId, version: null, ignoreDependencies: false, allowPrereleaseVersions: false);
        }

        public void InstallPackage(string packageId, SemanticVersion version) {
            InstallPackage(packageId, version, ignoreDependencies: false, allowPrereleaseVersions: false);
        }

        public virtual void InstallPackage(string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions) {
            IPackage package = PackageHelper.ResolvePackage(SourceRepository, LocalRepository, packageId, version, allowPrereleaseVersions);

            InstallPackage(package, ignoreDependencies, allowPrereleaseVersions);
        }

        public virtual void InstallPackage(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions) {
            Execute(package, new InstallWalker(LocalRepository,
                                               SourceRepository,
                                               Logger,
                                               ignoreDependencies,
                                               allowPrereleaseVersions));
        }

        private void Execute(IPackage package, IPackageOperationResolver resolver) {
            var operations = resolver.ResolveOperations(package);
            if (operations.Any()) {
                foreach (PackageOperation operation in operations) {
                    Execute(operation);
                }
            }
            else if (LocalRepository.Exists(package)) {
                Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyInstalled, package.GetFullName());
            }
        }

        protected void Execute(PackageOperation operation) {
            bool packageExists = LocalRepository.Exists(operation.Package);

            if (operation.Action == PackageAction.Install) {
                // If the package is already installed, then skip it
                if (packageExists) {
                    Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyInstalled, operation.Package.GetFullName());
                }
                else {
                    ExecuteInstall(operation.Package);
                }
            }
            else {
                if (packageExists) {
                    ExecuteUninstall(operation.Package);
                }
            }
        }

        protected void ExecuteInstall(IPackage package) {
            PackageOperationEventArgs args = CreateOperation(package);
            OnInstalling(args);

            if (args.Cancel) {
                return;
            }

            ExpandFiles(package);

            LocalRepository.AddPackage(package);

            Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageInstalledSuccessfully, package.GetFullName());

            OnInstalled(args);

            if (_cacheRepository != null) {
                _cacheRepository.AddPackage(package);
            }
        }

        private void ExpandFiles(IPackage package) {
            string packageDirectory = PathResolver.GetPackageDirectory(package);

            // Add files
            FileSystem.AddFiles(package.GetFiles(), packageDirectory);
        }

        public void UninstallPackage(string packageId) {
            UninstallPackage(packageId, version: null, forceRemove: false, removeDependencies: false);
        }

        public void UninstallPackage(string packageId, SemanticVersion version) {
            UninstallPackage(packageId, version: version, forceRemove: false, removeDependencies: false);
        }

        public void UninstallPackage(string packageId, SemanticVersion version, bool forceRemove) {
            UninstallPackage(packageId, version: version, forceRemove: forceRemove, removeDependencies: false);
        }

        public virtual void UninstallPackage(string packageId, SemanticVersion version, bool forceRemove, bool removeDependencies) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage package = LocalRepository.FindPackage(packageId, version: version);

            if (package == null) {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    NuGetResources.UnknownPackage, packageId));
            }

            UninstallPackage(package, forceRemove, removeDependencies);
        }

        public void UninstallPackage(IPackage package) {
            UninstallPackage(package, forceRemove: false, removeDependencies: false);
        }

        public void UninstallPackage(IPackage package, bool forceRemove) {
            UninstallPackage(package, forceRemove: forceRemove, removeDependencies: false);
        }

        public virtual void UninstallPackage(IPackage package, bool forceRemove, bool removeDependencies) {
            Execute(package, new UninstallWalker(LocalRepository,
                                                 new DependentsWalker(LocalRepository),
                                                 Logger,
                                                 removeDependencies,
                                                 forceRemove));
        }

        protected virtual void ExecuteUninstall(IPackage package) {
            PackageOperationEventArgs args = CreateOperation(package);
            OnUninstalling(args);

            if (args.Cancel) {
                return;
            }

            RemoveFiles(package);
            // Remove package to the repository
            LocalRepository.RemovePackage(package);

            Logger.Log(MessageLevel.Info, NuGetResources.Log_SuccessfullyUninstalledPackage, package.GetFullName());

            OnUninstalled(args);
        }

        private void RemoveFiles(IPackage package) {
            string packageDirectory = PathResolver.GetPackageDirectory(package);

            // Remove resource files
            FileSystem.DeleteFiles(package.GetFiles(), packageDirectory);
        }

        private void OnInstalling(PackageOperationEventArgs e) {
            if (_packageInstalling != null) {
                _packageInstalling(this, e);
            }
        }

        protected virtual void OnInstalled(PackageOperationEventArgs e) {
            if (_packageInstalled != null) {
                _packageInstalled(this, e);
            }
        }

        protected virtual void OnUninstalled(PackageOperationEventArgs e) {
            if (_packageUninstalled != null) {
                _packageUninstalled(this, e);
            }
        }

        private void OnUninstalling(PackageOperationEventArgs e) {
            if (_packageUninstalling != null) {
                _packageUninstalling(this, e);
            }
        }

        private PackageOperationEventArgs CreateOperation(IPackage package) {
            return new PackageOperationEventArgs(package, FileSystem, PathResolver.GetInstallPath(package));
        }

        public void UpdatePackage(string packageId, bool updateDependencies, bool allowPrereleaseVersions) {
            UpdatePackage(packageId, version: null, updateDependencies: updateDependencies, allowPrereleaseVersions: allowPrereleaseVersions);
        }

        public void UpdatePackage(string packageId, IVersionSpec versionSpec, bool updateDependencies, bool allowPrereleaseVersions) {
            UpdatePackage(packageId, () => SourceRepository.FindPackage(packageId, versionSpec, allowPrereleaseVersions),
                updateDependencies, allowPrereleaseVersions);
        }

        public void UpdatePackage(string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions) {
            UpdatePackage(packageId, () => SourceRepository.FindPackage(packageId, version, allowPrereleaseVersions),
                updateDependencies, allowPrereleaseVersions);
        }

        internal void UpdatePackage(string packageId, Func<IPackage> resolvePackage, bool updateDependencies, bool allowPrereleaseVersions) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage oldPackage = LocalRepository.FindPackage(packageId);

            // Check to see if this package is installed
            if (oldPackage == null) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.UnknownPackage, packageId));
            }

            Logger.Log(MessageLevel.Debug, NuGetResources.Debug_LookingForUpdates, packageId);

            IPackage newPackage = resolvePackage();

            if (newPackage != null && oldPackage.Version != newPackage.Version) {
                UpdatePackage(newPackage, updateDependencies, allowPrereleaseVersions);
            }
            else {
                Logger.Log(MessageLevel.Info, NuGetResources.Log_NoUpdatesAvailable, packageId);
            }
        }

        public void UpdatePackage(IPackage newPackage, bool updateDependencies, bool allowPrereleaseVersions) {
            Execute(newPackage, new UpdateWalker(LocalRepository,
                                                SourceRepository,
                                                new DependentsWalker(LocalRepository),
                                                NullConstraintProvider.Instance,
                                                Logger,
                                                updateDependencies,
                                                allowPrereleaseVersions));
        }
    }
}