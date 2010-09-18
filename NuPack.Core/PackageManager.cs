namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Internal.Web.Utils;
    using NuPack.Resources;

    public class PackageManager {
        private PackageEventListener _listener;

        public PackageManager(IPackageRepository sourceRepository, string path)
            : this(sourceRepository, new FileBasedProjectSystem(path)) {
        }

        public PackageManager(IPackageRepository sourceRepository, IFileSystem fileSystem) :
            this(sourceRepository, fileSystem, new LocalPackageRepository(fileSystem)) {
        }

        internal PackageManager(IPackageRepository sourceRepository, IFileSystem fileSystem, IPackageRepository localRepository) {
            SourceRepository = sourceRepository;
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

        public PackageEventListener Listener {
            get {
                return _listener ?? PackageEventListener.Default;
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

                ExecuteInstall(package);
            }
        }

        private void ExecuteInstall(IPackage package) {
            // notify listener before installing
            var context = new OperationContext(package, GetPackagePath(package));
            Listener.OnBeforeInstall(context);
            
            ExpandFiles(package);

            LocalRepository.AddPackage(package);

            // notify listener after installing
            Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_PackageInstalledSuccessfully, package.GetFullName());
            Listener.OnAfterInstall(context);
        }

        private void ExpandFiles(IPackage package) {
            string packageDirectory = Utility.GetPackageDirectory(package);

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
                ExecuteUninstall(package);
            }
        }

        private void ExecuteUninstall(IPackage package) {
            var context = new OperationContext(package, GetPackagePath(package));
            Listener.OnBeforeUninstall(context);

            RemoveFiles(package);

            // Remove package to the repository
            LocalRepository.RemovePackage(package);

            Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_SuccessfullyUninstalledPackage, package.GetFullName());
            Listener.OnAfterUninstall(context);
        }

        private void RemoveFiles(IPackage package) {
            string packageDirectory = Utility.GetPackageDirectory(package);

            // Remove resource files
            FileSystem.DeleteFiles(package.GetFiles(), packageDirectory, Listener);            
        }

        public bool IsPackageInstalled(IPackage package) {
            return LocalRepository.FindPackage(package.Id, exactVersion: package.Version) != null;
        }

        public string GetPackagePath(IPackage package) {
            if (package == null) {
                throw new ArgumentNullException("package");
            }
            return Path.Combine(FileSystem.Root, Utility.GetPackageDirectory(package));
        }
    }
}