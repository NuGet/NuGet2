using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Resolver;

namespace NuGet.Commands
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [Command(typeof(NuGetCommand), "install", "InstallCommandDescription",
        MinArgs = 0, MaxArgs = 1, UsageSummaryResourceName = "InstallCommandUsageSummary",
        UsageDescriptionResourceName = "InstallCommandUsageDescription",
        UsageExampleResourceName = "InstallCommandUsageExamples")]
    public class InstallCommand : DownloadCommandBase
    {
        private static readonly object _satelliteLock = new object();
        
        [Option(typeof(NuGetCommand), "InstallCommandOutputDirDescription")]
        public string OutputDirectory { get; set; }

        [Option(typeof(NuGetCommand), "InstallCommandVersionDescription")]
        public string Version { get; set; }

        [Option(typeof(NuGetCommand), "InstallCommandExcludeVersionDescription", AltName = "x")]
        public bool ExcludeVersion { get; set; }

        [Option(typeof(NuGetCommand), "InstallCommandPrerelease")]
        public bool Prerelease { get; set; }

        [Option(typeof(NuGetCommand), "InstallCommandRequireConsent")]
        public bool RequireConsent { get; set; }

        [Option(typeof(NuGetCommand), "InstallCommandSolutionDirectory")]
        public string SolutionDirectory { get; set; }        

        private bool AllowMultipleVersions
        {
            get { return !ExcludeVersion; }
        }

        [ImportingConstructor]
        public InstallCommand()
            : this(MachineCache.Default)
        {
        }

        protected internal InstallCommand(IPackageRepository cacheRepository) :
            base(cacheRepository)
        {
            // On mono, parallel builds are broken for some reason. See https://gist.github.com/4201936 for the errors
            // That are thrown.
            DisableParallelProcessing = EnvironmentUtility.IsMonoRuntime;
        }

        public override void ExecuteCommand()
        {
            CalculateEffectivePackageSaveMode();
            string installPath = ResolveInstallPath();
            IFileSystem fileSystem = CreateFileSystem(installPath);

            string configFilePath = Path.GetFullPath(Arguments.Count == 0 ? Constants.PackageReferenceFile : Arguments[0]);
            string configFileName = Path.GetFileName(configFilePath);

            // If the first argument is a packages.xxx.config file, install everything it lists
            // Otherwise, treat the first argument as a package Id
            if (PackageReferenceFile.IsValidConfigFileName(configFileName))
            {
                Prerelease = true;
                // By default the PackageReferenceFile does not throw if the file does not exist at the specified path.
                // We'll try reading from the file so that the file system throws a file not found
                EnsureFileExists(fileSystem, configFilePath);
                InstallPackagesFromConfigFile(fileSystem, GetPackageReferenceFile(configFilePath));
            }
            else
            {
                string packageId = Arguments[0];
                SemanticVersion version = Version != null ? new SemanticVersion(Version) : null;
                InstallPackage(fileSystem, packageId, version);
            }
        }

        protected virtual PackageReferenceFile GetPackageReferenceFile(string path)
        {
            return new PackageReferenceFile(Path.GetFullPath(path));
        }

        internal string ResolveInstallPath()
        {
            if (!String.IsNullOrEmpty(OutputDirectory))
            {
                // Use the OutputDirectory if specified.
                return OutputDirectory;
            }

            // If the SolutionDir is specified, use the .nuget directory under it to determine the solution-level settings
            ISettings currentSettings = Settings;
            if (!String.IsNullOrEmpty(SolutionDirectory))
            {
                var solutionSettingsFile = Path.Combine(SolutionDirectory.TrimEnd(Path.DirectorySeparatorChar), NuGetConstants.NuGetSolutionSettingsFolder);
                var fileSystem = CreateFileSystem(solutionSettingsFile);

                currentSettings = NuGet.Settings.LoadDefaultSettings(
                    fileSystem,
                    configFileName: null,
                    machineWideSettings: MachineWideSettings);

                // Recreate the source provider and credential provider
                SourceProvider = PackageSourceBuilder.CreateSourceProvider(currentSettings);
                HttpClient.DefaultCredentialProvider = new SettingsCredentialProvider(new ConsoleCredentialProvider(Console), SourceProvider, Console);
            }

            string installPath = currentSettings.GetRepositoryPath();
            if (!String.IsNullOrEmpty(installPath))
            {
                // If a value is specified in config, use that. 
                return installPath;
            }

            if (!String.IsNullOrEmpty(SolutionDirectory))
            {
                // For package restore scenarios, deduce the path of the packages directory from the solution directory.
                return Path.Combine(SolutionDirectory, CommandLineConstants.PackagesDirectoryName);
            }

            // Use the current directory as output.
            return Directory.GetCurrentDirectory();
        }

        private void InstallPackagesFromConfigFile(IFileSystem fileSystem, PackageReferenceFile configFile)
        {
            // display opt-out message if needed
            if (Console != null && RequireConsent && new PackageRestoreConsent(Settings).IsGranted)
            {
                string message = String.Format(
                    CultureInfo.CurrentCulture,
                    LocalizedResourceManager.GetString("RestoreCommandPackageRestoreOptOutMessage"),
                    NuGet.Resources.NuGetResources.PackageRestoreConsentCheckBoxText.Replace("&", ""));
                Console.WriteLine(message);
            }

            var packageReferences = CommandLineUtility.GetPackageReferences(configFile, requireVersion: true);

            bool installedAny = ExecuteInParallel(fileSystem, packageReferences);
            if (!installedAny && packageReferences.Any())
            {
                Console.WriteLine(LocalizedResourceManager.GetString("InstallCommandNothingToInstall"), Constants.PackageReferenceFile);
            }
        }

        /// <returns>True if one or more packages are installed.</returns>
        private bool ExecuteInParallel(IFileSystem fileSystem, ICollection<PackageReference> packageReferences)
        {
            bool packageRestoreConsent = new PackageRestoreConsent(Settings).IsGranted;
            int defaultConnectionLimit = ServicePointManager.DefaultConnectionLimit;
            if (packageReferences.Count > defaultConnectionLimit)
            {
                ServicePointManager.DefaultConnectionLimit = Math.Min(10, packageReferences.Count);
            }

            // The PackageSourceProvider reads from the underlying ISettings multiple times. One of the fields it reads is the password which is consequently decrypted
            // once for each package being installed. Per work item 2345, a couple of users are running into an issue where this results in an exception in native 
            // code. Instead, we'll use a cached set of sources. This should solve the issue and also give us some perf boost.
            SourceProvider = new CachedPackageSourceProvider(SourceProvider);

            var satellitePackages = new List<IPackage>();

            if (DisableParallelProcessing)
            {
                foreach (var package in packageReferences)
                {
                    RestorePackage(fileSystem, package.Id, package.Version, packageRestoreConsent, satellitePackages);
                }

                return true;
            }

            var tasks = packageReferences.Select(package =>
                            Task.Factory.StartNew(() => RestorePackage(fileSystem, package.Id, package.Version, packageRestoreConsent, satellitePackages))).ToArray();

            Task.WaitAll(tasks);
            // Return true if we installed any satellite packages or if any of our install tasks succeeded.
            return InstallSatellitePackages(fileSystem, satellitePackages) ||
                   tasks.All(p => !p.IsFaulted && p.Result);
        }

        private bool InstallSatellitePackages(IFileSystem fileSystem, List<IPackage> satellitePackages)
        {
            if (satellitePackages.Count == 0)
            {
                return false;
            }

            var packageManager = CreatePackageManager(fileSystem, AllowMultipleVersions);            
            var executor = new ActionExecutor();
            var operations = satellitePackages.Select(package => 
                new Resolver.PackageSolutionAction(PackageActionType.AddToPackagesFolder, package, packageManager));
            executor.Execute(operations);
            
            return true;
        }

        private bool RestorePackage(
            IFileSystem fileSystem,
            string packageId,
            SemanticVersion version,
            bool packageRestoreConsent,
            List<IPackage> satellitePackages)
        {
            var packageManager = CreatePackageManager(fileSystem, AllowMultipleVersions, checkDowngrade: false);
            if (IsPackageInstalled(packageManager.LocalRepository, fileSystem, packageId, version))
            {
                return false;
            }

            EnsurePackageRestoreConsent(packageRestoreConsent);
            using (packageManager.SourceRepository.StartOperation(
                RepositoryOperationNames.Restore, 
                packageId, 
                version == null ? null : version.ToString()))
            {
                var package = PackageHelper.ResolvePackage(packageManager.SourceRepository, packageId, version);
                if (package.IsSatellitePackage())
                {
                    // Satellite packages would necessarily have to be installed later than the corresponding package. 
                    // We'll collect them in a list to keep track and then install them later.
                    lock (_satelliteLock)
                    {
                        satellitePackages.Add(package);
                    }
                    return true;
                }

                // During package restore with parallel build, multiple projects would try to write to disk simultaneously which results in write contentions.
                // We work around this issue by ensuring only one instance of the exe installs the package.
                PackageExtractor.InstallPackage(packageManager, package);
                return true;
            }
        }

        private void InstallPackage(
            IFileSystem fileSystem,
            string packageId,
            SemanticVersion version)
        {
            if (version == null)
            {
                NoCache = true;
            }
            var packageManager = CreatePackageManager(fileSystem, AllowMultipleVersions);

            if (!PackageInstallNeeded(packageManager, packageId, version))
            {
                Console.WriteLine(LocalizedResourceManager.GetString("InstallCommandPackageAlreadyExists"), packageId);
                return;
            }

            if (version == null)
            {
                var latestVersion = GetLastestPackageVersion(
                    packageManager.SourceRepository, 
                    packageId, 
                    allowPrereleaseVersions: Prerelease);
                if (latestVersion != null)
                {
                    version = latestVersion.Version;
                }
            }

            using (packageManager.SourceRepository.StartOperation(
                RepositoryOperationNames.Install, 
                packageId, 
                version == null ? null : version.ToString()))
            {
                var resolver = new ActionResolver()
                {
                    Logger = Console,
                    AllowPrereleaseVersions = Prerelease
                };

                // Resolve the package to install
                IPackage package = PackageRepositoryHelper.ResolvePackage(
                    packageManager.SourceRepository,
                    packageManager.LocalRepository,
                    packageId,
                    version,
                    Prerelease);

                // Resolve operations. Note that we only care about AddToPackagesFolder actions
                resolver.AddOperation(
                    PackageAction.Install, 
                    package, 
                    new NullProjectManager(packageManager));                
                var actions = resolver.ResolveActions()
                    .Where(action => action.ActionType == PackageActionType.AddToPackagesFolder);

                if (actions.Any())
                {
                    var executor = new ActionExecutor()
                    {
                        Logger = Console
                    };
                    executor.Execute(actions);
                }
                else if (packageManager.LocalRepository.Exists(package))
                {
                    // If the package wasn't installed by our set of operations, notify the user.
                    Console.Log(
                        MessageLevel.Info, 
                        NuGet.Resources.NuGetResources.Log_PackageAlreadyInstalled, 
                        package.GetFullName());
                }
            }
        }

        /// <summary>
        /// Find the latest version of a package in the given repo.
        /// </summary>
        /// <param name="repo">The repository where to find the latest version of the package.</param>
        /// <param name="id">The id of the package.</param>
        /// <param name="allowPrereleaseVersions">Indicates if prerelease version is allowed.</param>
        /// <returns>the latest version of the package; or null if the package doesn't exist
        /// in the repo.</returns>
        private static IPackage GetLastestPackageVersion(IPackageRepository repo, string id, bool allowPrereleaseVersions)
        {
            IPackage latestVersion = null;
            var latestPackageLookup = repo as ILatestPackageLookup;
            if (latestPackageLookup != null &&
                latestPackageLookup.TryFindLatestPackageById(id, allowPrereleaseVersions, out latestVersion))
            {
                return latestVersion;
            }

            var aggregateRepository = repo as AggregateRepository;
            if (aggregateRepository != null)
            {
                return GetLatestVersionPackageByIdFromAggregateRepository(
                    aggregateRepository, id, allowPrereleaseVersions);
            }

            IEnumerable<IPackage> packages = repo.FindPackagesById(id).OrderByDescending(p => p.Version);
            if (!allowPrereleaseVersions)
            {
                packages = packages.Where(p => p.IsReleaseVersion());
            }

            latestVersion = packages.FirstOrDefault();
            return latestVersion;
        }

        /// <summary>
        /// Find the latest version of a package in the given aggregate repository.
        /// </summary>
        /// <param name="repo">The aggregate repository where to find the latest version of the package.</param>
        /// <param name="id">The id of the package.</param>
        /// <param name="allowPrereleaseVersions">Indicates if prerelease version is allowed.</param>
        /// <returns>the latest version of the package; or null if the package doesn't exist
        /// in the repo.</returns>
        private static IPackage GetLatestVersionPackageByIdFromAggregateRepository(
            AggregateRepository repo, string id,
            bool allowPrereleaseVersions)
        {
            var tasks = repo.Repositories.Select(p => Task.Factory.StartNew(
                state => GetLastestPackageVersion(p, id, allowPrereleaseVersions), p)).ToArray();

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException)
            {
                if (!repo.IgnoreFailingRepositories)
                {
                    throw;
                }
            }

            var versions = new List<IPackage>();
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                {
                    repo.LogRepository((IPackageRepository)task.AsyncState, task.Exception);
                }
                else if (task.Result != null)
                {
                    versions.Add(task.Result);
                }
            }

            return versions.OrderByDescending(v => v.Version).FirstOrDefault();
        }        

        /// <summary>
        /// Returns true if package install is needed.
        /// Package install is not needed if 
        /// - AllowMultipleVersions is false;
        /// - there is an existing package, and its version is newer than or equal to the 
        /// package to be installed.
        /// </summary>
        /// <param name="packageManager">The pacakge manager.</param>
        /// <param name="packageId">The id of the package to install.</param>
        /// <param name="version">The version of the package to install.</param>
        /// <returns>True if package install is neede; otherwise, false.</returns>
        private bool PackageInstallNeeded(
            IPackageManager packageManager,
            string packageId,
            SemanticVersion version)
        {
            if (AllowMultipleVersions)
            {
                return true;
            }

            var installedPackage = packageManager.LocalRepository.FindPackage(packageId);
            if (installedPackage == null)
            {
                return true;
            }

            if (version == null)
            {
                // need to query the source repository to get the version to be installed.
                IPackage package = packageManager.SourceRepository.FindPackage(
                    packageId, 
                    version,
                    NullConstraintProvider.Instance,
                    allowPrereleaseVersions: Prerelease, 
                    allowUnlisted: false);
                if (package == null)
                {
                    return false;
                }

                version = package.Version;
            }

            if (installedPackage.Version >= version)
            {
                // If the installed pacakge has newer version, no install is needed.
                return false;
            }

            // install is needed. In this case, uninstall the existing pacakge.
            var resolver = new ActionResolver()
            {
                Logger = packageManager.Logger,
                RemoveDependencies = true,
                ForceRemove = false
            };

            var projectManager = new NullProjectManager(packageManager);
            foreach (var package in packageManager.LocalRepository.GetPackages())
            {
                projectManager.LocalRepository.AddPackage(package);
            }
            resolver.AddOperation(
                PackageAction.Uninstall,
                installedPackage, 
                projectManager);
            var projectActions = resolver.ResolveActions();
            
            // because the projectManager's LocalRepository is not a PackageReferenceRepository,
            // the packages in the packages folder are not referenced. Thus, the resolved actions
            // are all PackageProjectActions. We need to create packages folder actions
            // from those PackageProjectActions.
            var solutionActions = new List<Resolver.PackageSolutionAction>();
            foreach (var action in projectActions)
            {
                var projectAction = action as PackageProjectAction;
                if (projectAction == null)
                {
                    continue;
                }

                var solutioAction = projectAction.ActionType == PackageActionType.Install ?
                    PackageActionType.AddToPackagesFolder :
                    PackageActionType.DeleteFromPackagesFolder;
                solutionActions.Add(new PackageSolutionAction(
                    solutioAction,                    
                    projectAction.Package,
                    packageManager));
            }

            var userOperationExecutor = new ActionExecutor()
            {
                Logger = packageManager.Logger
            };
            userOperationExecutor.Execute(solutionActions);
            return true;
        }

        protected internal virtual IFileSystem CreateFileSystem(string path)
        {
            path = Path.GetFullPath(path);
            return new PhysicalFileSystem(path);
        }

        private static void EnsureFileExists(IFileSystem fileSystem, string configFilePath)
        {
            using (fileSystem.OpenFile(configFilePath))
            {
                // Do nothing
            }
        }

        private void EnsurePackageRestoreConsent(bool packageRestoreConsent)
        {
            if (RequireConsent && !packageRestoreConsent)
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture,
                    LocalizedResourceManager.GetString("InstallCommandPackageRestoreConsentNotFound"),
                    NuGet.Resources.NuGetResources.PackageRestoreConsentCheckBoxText.Replace("&", ""));
                throw new InvalidOperationException(message);
            }
        }

        // Do a very quick check of whether a package in installed by checking whether the nupkg file exists
        private bool IsPackageInstalled(IPackageRepository repository, IFileSystem fileSystem, string packageId, SemanticVersion version)
        {
            if (!AllowMultipleVersions)
            {
                // If we allow side-by-side, we'll check if any version of a package is installed. This operation is expensive since it involves
                // reading package metadata, consequently we don't use this approach when side-by-side isn't used.
                return repository.Exists(packageId);
            }
            else if (version != null)
            {
                // If we know exactly what package to lookup, check if it's already installed locally. 
                // We'll do this by checking if the package directory exists on disk.
                var localRepository = repository as LocalPackageRepository;
                Debug.Assert(localRepository != null, "The PackageManager's local repository instance is necessarily a LocalPackageRepository instance.");
                var packagePaths = localRepository.GetPackageLookupPaths(packageId, version);
                return packagePaths.Any(fileSystem.FileExists);
            }
            return false;
        }
    }
}
