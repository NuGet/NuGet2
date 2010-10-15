namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.Internal.Web.Utils;
    using NuPack.Resources;

    public class PackageManager {
        private ILogger _logger;

        private event EventHandler<PackageOperationEventArgs> _packageInstalling;
        private event EventHandler<PackageOperationEventArgs> _packageInstalled;
        private event EventHandler<PackageOperationEventArgs> _packageUninstalling;
        private event EventHandler<PackageOperationEventArgs> _packageUninstalled;

        public PackageManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, string path)
            : this(sourceRepository, pathResolver, new FileBasedProjectSystem(path)) {
        }

        public PackageManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, IFileSystem fileSystem) :
            this(sourceRepository, pathResolver, fileSystem, new LocalPackageRepository(pathResolver, fileSystem)) {
        }

        internal PackageManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, IFileSystem fileSystem, IPackageRepository localRepository) {
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

            SourceRepository = sourceRepository;
            PathResolver = pathResolver;
            FileSystem = fileSystem;
            LocalRepository = localRepository;
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
            InstallPackage(packageId, version: null, ignoreDependencies: false);
        }

        public void InstallPackage(string packageId, Version version) {
            InstallPackage(packageId, version, ignoreDependencies: false);
        }

        public virtual void InstallPackage(string packageId, Version version, bool ignoreDependencies) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage package = SourceRepository.FindPackage(packageId, exactVersion: version);

            if (package == null) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuPackResources.UnknownPackage, packageId));
            }
            else {
                Logger.Log(MessageLevel.Info, NuPackResources.Log_AttemptingToInstallPackage, package.GetFullName());

                InstallPackage(package, ignoreDependencies);
            }
        }

        public virtual void InstallPackage(IPackage package, bool ignoreDependencies) {
            InstallPackage(package, new InstallWalker(LocalRepository,
                                                      SourceRepository,
                                                      Logger,
                                                      ignoreDependencies));
        }

        protected void InstallPackage(IPackage package, IPackageOperationResolver resolver) {
            Execute(resolver.ResolveOperations(package));
        }

        private void Execute(IEnumerable<PackageOperation> operations) {
            Debug.Assert(operations != null, "packages shouldn't be null");

            foreach (PackageOperation operation in operations) {                
                if (operation.Action == PackageAction.Install) {
                    // If the package is already installed, then skip it
                    if (LocalRepository.Exists(operation.Package)) {
                        Logger.Log(MessageLevel.Info, NuPackResources.Log_PackageAlreadyInstalled, operation.Package.GetFullName());
                        continue;
                    }

                    ExecuteInstall(operation.Package);
                }
                else {
                    ExecuteUninstall(operation.Package);
                }
            }
        }

        private void ExecuteInstall(IPackage package) {
            PackageOperationEventArgs args = CreateOperation(package);
            OnInstalling(args);

            if (args.Cancel) {
                return;
            }

            ExpandFiles(package);

            LocalRepository.AddPackage(package);

            Logger.Log(MessageLevel.Info, NuPackResources.Log_PackageInstalledSuccessfully, package.GetFullName());

            OnInstalled(args);
        }

        private void ExpandFiles(IPackage package) {
            string packageDirectory = PathResolver.GetPackageDirectory(package);

            // Add files files
            FileSystem.AddFiles(package.GetFiles(), packageDirectory);
        }

        public void UninstallPackage(string packageId) {
            UninstallPackage(packageId, version: null, forceRemove: false, removeDependencies: false);
        }

        public void UninstallPackage(string packageId, Version version) {
            UninstallPackage(packageId, version: version, forceRemove: false, removeDependencies: false);
        }

        public void UninstallPackage(string packageId, Version version, bool forceRemove) {
            UninstallPackage(packageId, version: version, forceRemove: forceRemove, removeDependencies: false);
        }

        public virtual void UninstallPackage(string packageId, Version version, bool forceRemove, bool removeDependencies) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage package = LocalRepository.FindPackage(packageId, exactVersion: version);

            if (package == null) {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    NuPackResources.UnknownPackage, packageId));
            }

            Logger.Log(MessageLevel.Info, NuPackResources.Log_AttemptingToUninstall, package.GetFullName());

            UninstallPackage(package, forceRemove, removeDependencies);
        }

        public void UninstallPackage(IPackage package) {
            UninstallPackage(package, forceRemove: false, removeDependencies: false);
        }

        public void UninstallPackage(IPackage package, bool forceRemove) {
            UninstallPackage(package, forceRemove: forceRemove, removeDependencies: false);
        }

        public virtual void UninstallPackage(IPackage package, bool forceRemove, bool removeDependencies) {
            UninstallPackage(package, new UninstallWalker(LocalRepository,
                                                          new DependentsResolver(LocalRepository),
                                                          Logger,
                                                          removeDependencies,
                                                          forceRemove));
        }

        public virtual void UninstallPackage(IPackage package, IPackageOperationResolver resolver) {
            Execute(resolver.ResolveOperations(package));
        }

        private void ExecuteUninstall(IPackage package) {
            PackageOperationEventArgs args = CreateOperation(package);
            OnUninstalling(args);

            if (args.Cancel) {
                return;
            }

            RemoveFiles(package);
            // Remove package to the repository
            LocalRepository.RemovePackage(package);

            Logger.Log(MessageLevel.Info, NuPackResources.Log_SuccessfullyUninstalledPackage, package.GetFullName());

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

        private void OnInstalled(PackageOperationEventArgs e) {
            if (_packageInstalled != null) {
                _packageInstalled(this, e);
            }
        }

        private void OnUninstalled(PackageOperationEventArgs e) {
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
            return new PackageOperationEventArgs(package, PathResolver.GetInstallPath(package));
        }
    }
}