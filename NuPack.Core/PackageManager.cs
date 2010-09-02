namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Internal.Web.Utils;
    using NuPack.Resources;

    public class PackageManager {
        private const string DefaultToolsDirectory = "tools";
        private const string DefaultReferencesDirectory = "lib";

        private PackageEventListener _listener;

        public PackageManager(IPackageRepository sourceRepository, string path, string referencesDirectory = DefaultReferencesDirectory)
            : this(sourceRepository, new FileBasedProjectSystem(path), referencesDirectory) {
        }

        public PackageManager(IPackageRepository sourceRepository, IFileSystem fileSystem, string referencesDirectory = DefaultReferencesDirectory) :
            this(sourceRepository, fileSystem, new LocalPackageRepository(fileSystem), referencesDirectory) {
        }

        internal PackageManager(IPackageRepository sourceRepository, IFileSystem fileSystem, IPackageRepository localRepository, string referencesDirectory = DefaultReferencesDirectory) {
            SourceRepository = sourceRepository;
            FileSystem = fileSystem;
            ReferencesDirectory = referencesDirectory;
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

        public string ReferencesDirectory { get; private set; }

        public PackageEventListener Listener {
            get {
                return _listener ?? PackageEventListener.Default;
            }
            set {
                _listener = value;
                FileSystem.Listener = value;
            }
        }

        public virtual void InstallPackage(string packageId, Version version = null, bool ignoreDependencies = false) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            Package package = FindPackageWithOptionalVersion(packageId, version);

            if (package == null) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuPackResources.UnknownPackage, packageId));
            }
            else {
                Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_AttemptingToInstallPackage, package);

                InstallPackage(package, ignoreDependencies);
            }
        }

        public virtual void InstallPackage(Package package, bool ignoreDependencies) {
            IEnumerable<Package> packages = null;

            if (ignoreDependencies) {
                packages = new[] { package };
            }
            else {
                packages = DependencyManager.ResolveDependenciesForInstall(package, LocalRepository, SourceRepository, Listener);
            }

            ApplyPackages(packages);
        }

        private void ApplyPackages(IEnumerable<Package> packages) {
            Debug.Assert(packages != null, "packages shouldn't be null");

            foreach (Package package in packages) {
                // If the package is already installed, then skip it
                if (LocalRepository.IsPackageInstalled(package)) {
                    Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_PackageAlreadyInstalled, package);
                    continue;
                }

                ExecuteInstall(package);
            }
        }

        private void ExecuteInstall(Package package) {
            // notify listener before installing
            var context = new OperationContext(package, GetPackagePath(package));
            Listener.OnBeforeInstall(context);

            LocalRepository.AddPackage(package);

            ExpandFiles(package);

            // notify listener after installing
            Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_PackageInstalledSuccessfully, package);
            Listener.OnAfterInstall(context);
        }

        private void ExpandFiles(Package package) {
            // Add tool files
            FileSystem.AddFiles(package.GetToolFiles(), GetToolPath(package), Listener);

            // Add content files
            FileSystem.AddFiles(package.GetContentFiles(), Utility.GetPackageDirectory(package), Listener);

            // Add the references to the reference path
            FileSystem.AddFiles(package.AssemblyReferences, GetReferencePath(package), Listener);
        }

        public virtual void UninstallPackage(string packageId, Version version = null, bool force = false, bool removeDependencies = false) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            Package package = LocalRepository.FindPackage(packageId, exactVersion: version);

            if (package == null) {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    NuPackResources.UnknownPackage, packageId));
            }

            Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_AttemptingToUninstall, package);

            UninstallPackage(package, force, removeDependencies);
        }

        public virtual void UninstallPackage(Package package, bool force = false, bool removeDependencies = false) {
            IEnumerable<Package> packages = DependencyManager.ResolveDependenciesForUninstall(package, LocalRepository, force, removeDependencies, Listener);

            RemovePackages(packages);
        }

        private void RemovePackages(IEnumerable<Package> packages) {
            Debug.Assert(packages != null, "packages should not be null");

            foreach (var package in packages) {
                ExecuteUninstall(package);
            }
        }

        private void ExecuteUninstall(Package package) {
            var context = new OperationContext(package, GetPackagePath(package));
            Listener.OnBeforeUninstall(context);

            RemoveFiles(package);

            // Remove package to the repository
            LocalRepository.RemovePackage(package);

            Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_SuccessfullyUninstalledPackage, package);
            Listener.OnAfterUninstall(context);
        }

        private void RemoveFiles(Package package) {
            // Remove tool files
            FileSystem.DeleteFiles(package.GetToolFiles(), GetToolPath(package), Listener);

            // Remove content files
            FileSystem.DeleteFiles(package.GetContentFiles(), Utility.GetPackageDirectory(package), Listener);

            // Removed the references and all of its files
            FileSystem.DeleteFiles(package.AssemblyReferences, GetReferencePath(package), Listener);

            // Delete the package directory if any
            FileSystem.DeleteDirectory(Utility.GetPackageDirectory(package), true);
        }

        protected Package FindPackageWithOptionalVersion(string packageId, Version version) {
            return SourceRepository.FindPackage(packageId, exactVersion: version);
        }

        public bool IsPackageInstalled(Package package) {
            return LocalRepository.FindPackage(package.Id, exactVersion: package.Version) != null;
        }

        private string GetReferencePath(Package package) {
            return Path.Combine(Utility.GetPackageDirectory(package), ReferencesDirectory);
        }

        private static string GetToolPath(Package package) {
            return Path.Combine(Utility.GetPackageDirectory(package), DefaultToolsDirectory);
        }

        public string GetPackagePath(Package package) {
            if (package == null) {
                throw new ArgumentNullException("package");
            }
            return Path.Combine(FileSystem.Root, Utility.GetPackageDirectory(package));
        }
    }
}