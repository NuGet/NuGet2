namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.Internal.Web.Utils;
    using NuPack.Resources;

    public class PackageManager {
        private IPackageEventListener _listener;

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

        protected IFileSystem FileSystem {
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

        public IPackageEventListener Listener {
            get {
                return _listener ?? DefaultPackageEventListener.Instance;
            }
            set {
                _listener = value;
                FileSystem.Listener = value;
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
                Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_AttemptingToInstallPackage, package.GetFullName());

                InstallPackage(package, ignoreDependencies);
            }
        }

        public virtual void InstallPackage(IPackage package, bool ignoreDependencies) {
            IEnumerable<IPackage> packages = null;

            if (ignoreDependencies) {
                packages = new[] { package };
            }
            else {
                packages = DependencyManager.ResolveDependenciesForInstall(package, LocalRepository, SourceRepository, Listener);
            }

            ApplyPackages(packages);
        }

        private void ApplyPackages(IEnumerable<IPackage> packages) {
            Debug.Assert(packages != null, "packages shouldn't be null");

            foreach (IPackage package in packages) {
                // If the package is already installed, then skip it
                if (LocalRepository.IsPackageInstalled(package)) {
                    Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_PackageAlreadyInstalled, package.GetFullName());
                    continue;
                }

                ExecuteInstall(CreateOperationContext(package));
            }
        }

        private void ExecuteInstall(OperationContext context) {
            // Notify listener before installing
            Listener.OnBeforeInstall(context);

            ExpandFiles(context.Package);

            LocalRepository.AddPackage(context.Package);

            // notify listener after installing
            Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_PackageInstalledSuccessfully, context.Package.GetFullName());
            Listener.OnAfterInstall(context);
        }

        private void ExpandFiles(IPackage package) {
            string packageDirectory = PathResolver.GetPackageDirectory(package);

            // Add files files
            FileSystem.AddFiles(package.GetFiles(), packageDirectory, Listener);
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

            Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_AttemptingToUninstall, package.GetFullName());

            UninstallPackage(package, forceRemove, removeDependencies);
        }

        public void UninstallPackage(IPackage package) {
            UninstallPackage(package, forceRemove: false, removeDependencies: false);
        }

        public void UninstallPackage(IPackage package, bool forceRemove) {
            UninstallPackage(package, forceRemove: forceRemove, removeDependencies: false);
        }

        public virtual void UninstallPackage(IPackage package, bool forceRemove, bool removeDependencies) {
            IEnumerable<IPackage> packages = DependencyManager.ResolveDependenciesForUninstall(package, LocalRepository, forceRemove, removeDependencies, Listener);

            RemovePackages(packages);
        }

        private void RemovePackages(IEnumerable<IPackage> packages) {
            Debug.Assert(packages != null, "packages should not be null");

            foreach (var package in packages) {
                ExecuteUninstall(CreateOperationContext(package));
            }
        }

        private void ExecuteUninstall(OperationContext context) {
            Listener.OnBeforeUninstall(context);

            RemoveFiles(context.Package);

            // Remove package to the repository
            LocalRepository.RemovePackage(context.Package);

            Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_SuccessfullyUninstalledPackage, context.Package.GetFullName());
            Listener.OnAfterUninstall(context);
        }

        private void RemoveFiles(IPackage package) {
            string packageDirectory = PathResolver.GetPackageDirectory(package);

            // Remove resource files
            FileSystem.DeleteFiles(package.GetFiles(), packageDirectory, Listener);
        }

        public bool IsPackageInstalled(IPackage package) {
            return LocalRepository.FindPackage(package.Id, exactVersion: package.Version) != null;
        }

        private OperationContext CreateOperationContext(IPackage package) {
            return new OperationContext(package, PathResolver.GetInstallPath(package));
        }
    }
}