using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Resources;

namespace NuGet
{
    public class PackageManager : IPackageManager
    {
        private ILogger _logger;

        public event EventHandler<PackageOperationEventArgs> PackageInstalling;
        public event EventHandler<PackageOperationEventArgs> PackageInstalled;
        public event EventHandler<PackageOperationEventArgs> PackageUninstalling;
        public event EventHandler<PackageOperationEventArgs> PackageUninstalled;

        public PackageManager(IPackageRepository sourceRepository, string path)
            : this(sourceRepository, new DefaultPackagePathResolver(path), new PhysicalFileSystem(path))
        {
        }

        public PackageManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, IFileSystem fileSystem) :
            this(sourceRepository, pathResolver, fileSystem, new LocalPackageRepository(pathResolver, fileSystem))
        {
        }

        public PackageManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, IFileSystem fileSystem, IPackageRepository localRepository)
        {
            if (sourceRepository == null)
            {
                throw new ArgumentNullException("sourceRepository");
            }
            if (pathResolver == null)
            {
                throw new ArgumentNullException("pathResolver");
            }
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            if (localRepository == null)
            {
                throw new ArgumentNullException("localRepository");
            }

            SourceRepository = sourceRepository;
            PathResolver = pathResolver;
            FileSystem = fileSystem;
            LocalRepository = localRepository;
            DependencyVersion = DependencyVersion.Lowest;
            CheckDowngrade = true;
        }

        public IFileSystem FileSystem
        {
            get;
            set;
        }

        public IPackageRepository SourceRepository
        {
            get;
            private set;
        }

        public IPackageRepository LocalRepository
        {
            get;
            private set;
        }

        public IPackagePathResolver PathResolver
        {
            get;
            private set;
        }

        public ILogger Logger
        {
            get
            {
                return _logger ?? NullLogger.Instance;
            }
            set
            {
                _logger = value;
            }
        }

        public DependencyVersion DependencyVersion
        {
            get;
            set;
        }

        public bool WhatIf
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value that determines if the walk operation during install
        /// verifies the targetability (Project \ External) of the package.
        /// </summary>
        public bool SkipPackageTargetCheck { get; set; }

        public void InstallPackage(string packageId)
        {
            InstallPackage(packageId, version: null, ignoreDependencies: false, allowPrereleaseVersions: false);
        }

        public void InstallPackage(string packageId, SemanticVersion version)
        {
            InstallPackage(packageId: packageId, version: version, ignoreDependencies: false, allowPrereleaseVersions: false);
        }

        public virtual void InstallPackage(string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions)
        {
            IPackage package = PackageRepositoryHelper.ResolvePackage(SourceRepository, LocalRepository, packageId, version, allowPrereleaseVersions);

            InstallPackage(package, ignoreDependencies, allowPrereleaseVersions);
        }

        public virtual void InstallPackage(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
        {
            InstallPackage(package, targetFramework: null, ignoreDependencies: ignoreDependencies, allowPrereleaseVersions: allowPrereleaseVersions);
        }

        public void InstallPackage(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions, bool ignoreWalkInfo)
        {
            InstallPackage(package, targetFramework: null, ignoreDependencies: ignoreDependencies, allowPrereleaseVersions: allowPrereleaseVersions, ignoreWalkInfo: ignoreWalkInfo);
        }

        protected void InstallPackage(
            IPackage package,
            FrameworkName targetFramework,
            bool ignoreDependencies,
            bool allowPrereleaseVersions,
            bool ignoreWalkInfo = false)
        {
            if (WhatIf)
            {
                // This prevents InstallWalker from downloading the packages
                ignoreWalkInfo = true;
            }

            var installerWalker = new InstallWalker(
                LocalRepository, SourceRepository,
                targetFramework, Logger,
                ignoreDependencies, allowPrereleaseVersions,
                DependencyVersion)
            {
                DisableWalkInfo = ignoreWalkInfo,
                CheckDowngrade = CheckDowngrade,
                SkipPackageTargetCheck = SkipPackageTargetCheck
            };
            Execute(package, installerWalker);
        }

        private void Execute(IPackage package, IPackageOperationResolver resolver)
        {
            var operations = resolver.ResolveOperations(package);
            if (operations.Any())
            {
                foreach (PackageOperation operation in operations)
                {
                    Execute(operation);
                }
            }
            else if (LocalRepository.Exists(package))
            {
                // If the package wasn't installed by our set of operations, notify the user.
                Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyInstalled, package.GetFullName());
            }
        }

        protected void Execute(PackageOperation operation)
        {
            bool packageExists = LocalRepository.Exists(operation.Package);

            if (operation.Action == PackageAction.Install)
            {
                // If the package is already installed, then skip it
                if (packageExists)
                {
                    Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyInstalled, operation.Package.GetFullName());
                }
                else
                {
                    if (WhatIf)
                    {
                        Logger.Log(MessageLevel.Info, NuGetResources.Log_InstallPackage, operation.Package);
                    }
                    else
                    {
                        ExecuteInstall(operation.Package);
                    }
                }
            }
            else
            {
                if (packageExists)
                {
                    if (WhatIf)
                    {
                        Logger.Log(MessageLevel.Info, NuGetResources.Log_UninstallPackage, operation.Package);
                    }
                    else
                    {
                        ExecuteUninstall(operation.Package);
                    }
                }
            }
        }

        protected void ExecuteInstall(IPackage package)
        {
            string packageFullName = package.GetFullName();
            Logger.Log(MessageLevel.Info, NuGetResources.Log_BeginInstallPackage, packageFullName);

            PackageOperationEventArgs args = CreateOperation(package);
            OnInstalling(args);

            if (args.Cancel)
            {
                return;
            }

            OnExpandFiles(args);

            LocalRepository.AddPackage(package);

            Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageInstalledSuccessfully, packageFullName);

            OnInstalled(args);
        }

        private void ExpandFiles(IPackage package)
        {
            var batchProcessor = FileSystem as IBatchProcessor<string>;
            try
            {
                var files = package.GetFiles().ToList();
                if (batchProcessor != null)
                {
                    // Notify the batch processor that the files are being added. This is to allow source controlled file systems 
                    // to manage previously uninstalled files.
                    batchProcessor.BeginProcessing(files.Select(p => p.Path), PackageAction.Install);
                }

                string packageDirectory = PathResolver.GetPackageDirectory(package);

                // Add files
                FileSystem.AddFiles(files, packageDirectory);

                ExpandSatellitePackageFiles(package);
            }
            finally
            {
                if (batchProcessor != null)
                {
                    batchProcessor.EndProcessing();
                }
            }
        }

        protected void ExpandSatellitePackageFiles(IPackage package)
        {
            // If this is a Satellite Package, then copy the satellite files into the related runtime package folder too
            IPackage runtimePackage;
            if (PackageHelper.IsSatellitePackage(package, LocalRepository, targetFramework: null, runtimePackage: out runtimePackage))
            {
                var satelliteFiles = package.GetSatelliteFiles();
                var runtimePath = PathResolver.GetPackageDirectory(runtimePackage);
                FileSystem.AddFiles(satelliteFiles, runtimePath);
            }
        }

        public void UninstallPackage(string packageId)
        {
            UninstallPackage(packageId, version: null, forceRemove: false, removeDependencies: false);
        }

        public void UninstallPackage(string packageId, SemanticVersion version)
        {
            UninstallPackage(packageId, version: version, forceRemove: false, removeDependencies: false);
        }

        public void UninstallPackage(string packageId, SemanticVersion version, bool forceRemove)
        {
            UninstallPackage(packageId, version: version, forceRemove: forceRemove, removeDependencies: false);
        }

        public virtual void UninstallPackage(string packageId, SemanticVersion version, bool forceRemove, bool removeDependencies)
        {
            if (String.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage package = LocalRepository.FindPackage(packageId, version: version);

            if (package == null)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    NuGetResources.UnknownPackage, packageId));
            }

            UninstallPackage(package, forceRemove, removeDependencies);
        }

        public void UninstallPackage(IPackage package)
        {
            UninstallPackage(package, forceRemove: false, removeDependencies: false);
        }

        public void UninstallPackage(IPackage package, bool forceRemove)
        {
            UninstallPackage(package, forceRemove: forceRemove, removeDependencies: false);
        }

        public virtual void UninstallPackage(IPackage package, bool forceRemove, bool removeDependencies)
        {
            Execute(package, new UninstallWalker(LocalRepository,
                                                 new DependentsWalker(LocalRepository, targetFramework: null),
                                                 targetFramework: null,
                                                 logger: Logger,
                                                 removeDependencies: removeDependencies,
                                                 forceRemove: forceRemove)
                                                 {
                                                     DisableWalkInfo = WhatIf
                                                 });
        }

        protected virtual void ExecuteUninstall(IPackage package)
        {
            string packageFullName = package.GetFullName();
            Logger.Log(MessageLevel.Info, NuGetResources.Log_BeginUninstallPackage, packageFullName);

            PackageOperationEventArgs args = CreateOperation(package);
            OnUninstalling(args);

            if (args.Cancel)
            {
                return;
            }

            OnRemoveFiles(args);

            LocalRepository.RemovePackage(package);

            Logger.Log(MessageLevel.Info, NuGetResources.Log_SuccessfullyUninstalledPackage, packageFullName);

            OnUninstalled(args);
        }

        private void RemoveFiles(IPackage package)
        {
            string packageDirectory = PathResolver.GetPackageDirectory(package);

            // If this is a Satellite Package, then remove the files from the related runtime package folder too
            IPackage runtimePackage;
            if (PackageHelper.IsSatellitePackage(package, LocalRepository, targetFramework: null, runtimePackage: out runtimePackage))
            {
                var satelliteFiles = package.GetSatelliteFiles();
                var runtimePath = PathResolver.GetPackageDirectory(runtimePackage);
                FileSystem.DeleteFiles(satelliteFiles, runtimePath);
            }

            // Remove package files
            // IMPORTANT: This has to be done AFTER removing satellite files from runtime package,
            // because starting from 2.1, we read satellite files directly from package files, instead of .nupkg
            FileSystem.DeleteFiles(package.GetFiles(), packageDirectory);
        }

        protected virtual void OnInstalling(PackageOperationEventArgs e)
        {
            if (PackageInstalling != null)
            {
                PackageInstalling(this, e);
            }
        }

        protected virtual void OnExpandFiles(PackageOperationEventArgs e)
        {
            ExpandFiles(e.Package);
        }

        protected virtual void OnInstalled(PackageOperationEventArgs e)
        {
            if (PackageInstalled != null)
            {
                PackageInstalled(this, e);
            }
        }

        protected virtual void OnUninstalling(PackageOperationEventArgs e)
        {
            if (PackageUninstalling != null)
            {
                PackageUninstalling(this, e);
            }
        }

        protected virtual void OnRemoveFiles(PackageOperationEventArgs e)
        {
            RemoveFiles(e.Package);
        }

        protected virtual void OnUninstalled(PackageOperationEventArgs e)
        {
            if (PackageUninstalled != null)
            {
                PackageUninstalled(this, e);
            }
        }

        private PackageOperationEventArgs CreateOperation(IPackage package)
        {
            return new PackageOperationEventArgs(package, FileSystem, PathResolver.GetInstallPath(package));
        }

        public void UpdatePackage(string packageId, bool updateDependencies, bool allowPrereleaseVersions)
        {
            UpdatePackage(packageId, version: null, updateDependencies: updateDependencies, allowPrereleaseVersions: allowPrereleaseVersions);
        }

        public void UpdatePackage(string packageId, IVersionSpec versionSpec, bool updateDependencies, bool allowPrereleaseVersions)
        {
            UpdatePackage(packageId, () => SourceRepository.FindPackage(packageId, versionSpec, allowPrereleaseVersions, allowUnlisted: false),
                updateDependencies, allowPrereleaseVersions);
        }

        public void UpdatePackage(string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions)
        {
            UpdatePackage(packageId, () => SourceRepository.FindPackage(packageId, version, allowPrereleaseVersions, allowUnlisted: false),
                updateDependencies, allowPrereleaseVersions);
        }

        internal void UpdatePackage(string packageId, Func<IPackage> resolvePackage, bool updateDependencies, bool allowPrereleaseVersions)
        {
            if (String.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage oldPackage = LocalRepository.FindPackage(packageId);

            // Check to see if this package is installed
            if (oldPackage == null)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.UnknownPackage, packageId));
            }

            Logger.Log(MessageLevel.Debug, NuGetResources.Debug_LookingForUpdates, packageId);

            IPackage newPackage = resolvePackage();

            if (newPackage != null && oldPackage.Version != newPackage.Version)
            {
                UpdatePackage(newPackage, updateDependencies, allowPrereleaseVersions);
            }
            else
            {
                Logger.Log(MessageLevel.Info, NuGetResources.Log_NoUpdatesAvailable, packageId);
            }
        }

        public void UpdatePackage(IPackage newPackage, bool updateDependencies, bool allowPrereleaseVersions)
        {
            Execute(newPackage, new UpdateWalker(LocalRepository,
                                                SourceRepository,
                                                new DependentsWalker(LocalRepository, targetFramework: null),
                                                NullConstraintProvider.Instance,
                                                targetFramework: null,
                                                logger: Logger,
                                                updateDependencies: updateDependencies,
                                                allowPrereleaseVersions: allowPrereleaseVersions));
        }

        public bool CheckDowngrade { get; set; }
    }
}