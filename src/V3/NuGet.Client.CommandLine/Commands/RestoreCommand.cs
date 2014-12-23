using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
//using Microsoft.Build.Exceptions;
using NuGet.Common;
using NuGet.Resolver;
using NuGet.Versioning;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;
using NuGet.Client;
using NuGet.Client.Installation;
using System.Threading;
using Newtonsoft.Json.Linq;
using NuGet.Client.Interop;
using System.Diagnostics;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommandResourceType), "restore", "RestoreCommandDescription",
        MinArgs = 0, MaxArgs = 1, UsageSummaryResourceName = "RestoreCommandUsageSummary",
        UsageDescriptionResourceName = "RestoreCommandUsageDescription",
        UsageExampleResourceName = "RestoreCommandUsageExamples")]
    public class RestoreCommand : DownloadCommandBase
    {
        // True means we're restoring for a solution; False means we're restoring packages
        // listed in a packages.config file.
        private bool _restoringForSolution;

        private string _solutionFileFullPath;
        private string _packagesConfigFileFullPath;

        // A flag indicating if the opt-out message should be displayed.
        private bool _outputOptOutMessage;

        // lock used to access _outputOptOutMessage.
        private readonly object _outputOptOutMessageLock = new object();

        [Option(typeof(NuGetCommandResourceType), "RestoreCommandRequireConsent")]
        public bool RequireConsent { get; set; }

        [Option(typeof(NuGetCommandResourceType), "RestoreCommandPackagesDirectory", AltName = "OutputDirectory")]
        public string PackagesDirectory { get; set; }

        [Option(typeof(NuGetCommandResourceType), "RestoreCommandSolutionDirectory")]
        public string SolutionDirectory { get; set; }

        /// <remarks>
        /// Meant for unit testing.
        /// </remarks>
        internal bool RestoringForSolution
        {
            get { return _restoringForSolution; }
        }

        /// <remarks>
        /// Meant for unit testing.
        /// </remarks>
        internal string SolutionFileFullPath
        {
            get { return _solutionFileFullPath; }
        }

        /// <remarks>
        /// Meant for unit testing.
        /// </remarks>
        internal string PackagesConfigFileFullPath
        {
            get { return _packagesConfigFileFullPath; }
        }

        [ImportingConstructor]
        public RestoreCommand()
            : this(MachineCache.Default)
        {
        }

        protected internal RestoreCommand(IPackageRepository cacheRepository) :
            base(cacheRepository)
        {
            _outputOptOutMessage = true;
        }

        internal void DetermineRestoreMode()
        {
            if (Arguments.Count == 0)
            {
                // look for solution files first
                _solutionFileFullPath = GetSolutionFile("");
                if (_solutionFileFullPath != null)
                {
                    _restoringForSolution = true;
                    if (Verbosity == Verbosity.Detailed)
                    {
                        Console.WriteLine(LocalizedResourceManager.GetString("RestoreCommandRestoringPackagesForSolution"), _solutionFileFullPath);
                    }

                    return;
                }

                // look for packages.config file
                if (FileSystem.FileExists(Constants.PackageReferenceFile))
                {
                    _restoringForSolution = false;
                    _packagesConfigFileFullPath = FileSystem.GetFullPath(Constants.PackageReferenceFile);
                    if (Verbosity == NuGet.Verbosity.Detailed)
                    {
                        Console.WriteLine(LocalizedResourceManager.GetString("RestoreCommandRestoringPackagesFromPackagesConfigFile"));
                    }

                    return;
                }

                throw new InvalidOperationException(LocalizedResourceManager.GetString("Error_NoSolutionFileNorePackagesConfigFile"));
            }
            else
            {
                if (Path.GetFileName(Arguments[0]).Equals(Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase))
                {
                    // restoring from packages.config file
                    _restoringForSolution = false;
                    _packagesConfigFileFullPath = FileSystem.GetFullPath(Arguments[0]);
                }
                else
                {
                    _restoringForSolution = true;
                    _solutionFileFullPath = GetSolutionFile(Arguments[0]);
                    if (_solutionFileFullPath == null)
                    {
                        throw new InvalidOperationException(LocalizedResourceManager.GetString("Error_CannotLocateSolutionFile"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the solution file, in full path format. If <paramref name="solutionFileOrDirectory"/> is a file, 
        /// that file is returned. Otherwise, searches for a *.sln file in
        /// directory <paramref name="solutionFileOrDirectory"/>. If exactly one sln file is found, 
        /// that file is returned. If multiple sln files are found, an exception is thrown. 
        /// If no sln files are found, returns null.
        /// </summary>
        /// <param name="solutionFileOrDirectory">The solution file or directory to search for solution files.</param>
        /// <returns>The full path of the solution file. Or null if no solution file can be found.</returns>
        private string GetSolutionFile(string solutionFileOrDirectory)
        {
            if (FileSystem.FileExists(solutionFileOrDirectory))
            {
                return FileSystem.GetFullPath(solutionFileOrDirectory);
            }

            // look for solution files
            var slnFiles = FileSystem.GetFiles(solutionFileOrDirectory, "*.sln").ToArray();
            if (slnFiles.Length > 1)
            {
                throw new InvalidOperationException(LocalizedResourceManager.GetString("Error_MultipleSolutions"));
            }

            if (slnFiles.Length == 1)
            {
                return FileSystem.GetFullPath(slnFiles[0]);
            }

            return null;
        }

        protected internal virtual IFileSystem CreateFileSystem(string path)
        {
            path = FileSystem.GetFullPath(path);
            return new PhysicalFileSystem(path);
        }

        private void ReadSettings()
        {
            if (_restoringForSolution || !String.IsNullOrEmpty(SolutionDirectory))
            {
                var solutionDirectory = _restoringForSolution ?
                    Path.GetDirectoryName(_solutionFileFullPath) :
                    SolutionDirectory;

                // Read the solution-level settings
                var solutionSettingsFile = Path.Combine(
                    solutionDirectory,
                    NuGetConstants.NuGetSolutionSettingsFolder);
                var fileSystem = CreateFileSystem(solutionSettingsFile);

                if (ConfigFile != null)
                {
                    ConfigFile = FileSystem.GetFullPath(ConfigFile);
                }

                Settings = NuGet.Settings.LoadDefaultSettings(
                    fileSystem: fileSystem,
                    configFileName: ConfigFile,
                    machineWideSettings: MachineWideSettings);

                // Recreate the source provider and credential provider
                SourceProvider = PackageSourceBuilder.CreateSourceProvider(Settings);
                HttpClient.DefaultCredentialProvider = new SettingsCredentialProvider(new ConsoleCredentialProvider(Console), SourceProvider, Console);
            }
        }

        private string GetPackagesFolder()
        {
            if (!String.IsNullOrEmpty(PackagesDirectory))
            {
                return PackagesDirectory;
            }

            var repositoryPath = Settings.GetRepositoryPath();
            if (!String.IsNullOrEmpty(repositoryPath))
            {
                return repositoryPath;
            }

            if (!String.IsNullOrEmpty(SolutionDirectory))
            {
                return Path.Combine(SolutionDirectory, CommandLineConstants.PackagesDirectoryName);
            }

            if (_restoringForSolution)
            {
                return Path.Combine(Path.GetDirectoryName(_solutionFileFullPath), CommandLineConstants.PackagesDirectoryName);
            }

            throw new InvalidOperationException(LocalizedResourceManager.GetString("RestoreCommandCannotDeterminePackagesFolder"));
        }

        // BUGBUG: NEED to use NuGet.Versioning.NuGetVersion and not the old SemanticVersion
        // Do a very quick check of whether a package in installed by checking whether the nupkg file exists
        private static bool IsPackageInstalled(IPackageRepository repository, IFileSystem packagesFolderFileSystem, PackageIdentity packageIdentity)
        {
            if (packageIdentity.Version != null)
            {
                // If we know exactly what package to lookup, check if it's already installed locally. 
                // We'll do this by checking if the package directory exists on disk.
                var localRepository = (LocalPackageRepository)repository;
                var packagePaths = localRepository.GetPackageLookupPaths(packageIdentity.Id, new SemanticVersion(packageIdentity.Version.ToString()));
                return packagePaths.Any(packagesFolderFileSystem.FileExists);
            }
            return false;
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

        private bool RestorePackage(
            IFileSystem packagesFolderFileSystem,
            PackageIdentity packageIdentity,
            bool packageRestoreConsent,
            ConcurrentQueue<JObject> satellitePackages)
        {
            // BUGBUG: Looks like we are creating PackageManager for every single restore. This is likely done to support execution in parallel
            var packageManager = CreatePackageManager(packagesFolderFileSystem, useSideBySidePaths: true);
            if (IsPackageInstalled(packageManager.LocalRepository, packagesFolderFileSystem, packageIdentity))
            {
                return false;
            }

            // BUGBUG: Investigate if the following lock is needed
            EnsurePackageRestoreConsent(packageRestoreConsent);
            if (RequireConsent && _outputOptOutMessage)
            {
                lock (_outputOptOutMessageLock)
                {
                    if (_outputOptOutMessage)
                    {
                        string message = String.Format(
                            CultureInfo.CurrentCulture,
                            LocalizedResourceManager.GetString("RestoreCommandPackageRestoreOptOutMessage"),
                            NuGet.Resources.NuGetResources.PackageRestoreConsentCheckBoxText.Replace("&", ""));
                        Console.WriteLine(message);
                        _outputOptOutMessage = false;
                    }
                }
            }

            SourceRepository = SourceRepositoryHelper.CreateSourceRepository(SourceProvider, Source);

            // BUGBUG: TO BE REMOVED AFTER INVESTIGATION
            using (packageManager.SourceRepository.StartOperation(
                RepositoryOperationNames.Restore,
                packageIdentity.Id,
                packageIdentity.Version == null ? null : packageIdentity.Version.ToString()))
            {
                // BUGBUG: Satellite packages should only be restored at the end. Find out Why first??
                //         And, then, handle it here


                var filesystemInstallationTarget = new FilesystemInstallationTarget(packageManager);

                // BUGBUG: Should consider using async method and await
                var task = SourceRepository.GetPackageMetadata(packageIdentity.Id, packageIdentity.Version);
                task.Wait();
                var packageJSON = task.Result;

                if (IsSatellitePackage(packageJSON))
                {
                    // Satellite packages would necessarily have to be installed later than the corresponding package. 
                    // We'll collect them in a list to keep track and then install them later.
                    satellitePackages.Enqueue(packageJSON);
                    return true;
                }

                var packageAction = new NewPackageAction(NuGet.Client.PackageActionType.Download,
                    packageIdentity, packageJSON, filesystemInstallationTarget, SourceRepository, null);

                // BUGBUG: See PackageExtractor.cs for locking mechanism used to handle concurrency
                NuGet.Client.Installation.ActionExecutor actionExecutor = new Client.Installation.ActionExecutor();

                // BUGBUG: This is likely inefficient. Consider collecting all actions first and executing them in 1 shot
                var packageActions = new List<NewPackageAction>() { packageAction };
                actionExecutor.ExecuteActions(packageActions, Console);
                return true;
            }
        }

        private bool IsSatellitePackage(JObject packageJSON)
        {
            if (!String.IsNullOrEmpty(packageJSON[Properties.PackageId].ToString()) &&
                    packageJSON[Properties.PackageId].ToString().EndsWith('.' + packageJSON[Properties.Language].ToString(), StringComparison.OrdinalIgnoreCase))
            {
                // The satellite pack's Id is of the format <Core-Package-Id>.<Language>. Extract the core package id using this.
                // Additionally satellite packages have a strict dependency on the core package
                IEnumerable <PackageDependencySet> depEnum;
                var deps = packageJSON.Value<JArray>(Properties.DependencyGroups);
                if (deps == null)
                {
                    depEnum = Enumerable.Empty<PackageDependencySet>();
                }
                else
                {
                    depEnum = deps.Select(t => PackageJsonLd.DependencySetFromJson((JObject)t));
                }
                string corePackageId = packageJSON[Properties.PackageId].ToString().Substring(0, packageJSON[Properties.PackageId].ToString().Length - packageJSON[Properties.Language].ToString().Length - 1);
                return depEnum.SelectMany(s => s.Dependencies).Any(
                       d => d.Id.Equals(corePackageId, StringComparison.OrdinalIgnoreCase) &&
                       d.VersionSpec != null &&
                       d.VersionSpec.MaxVersion == d.VersionSpec.MinVersion && d.VersionSpec.IsMaxInclusive && d.VersionSpec.IsMinInclusive);
            }
            return false;
        }

        /// <returns>True if one or more packages are installed.</returns>
        private bool ExecuteInParallel(IFileSystem fileSystem, ICollection<InstalledPackageReference> installedPackageReferences)
        {
            bool packageRestoreConsent = new PackageRestoreConsent(Settings).IsGranted;
            int defaultConnectionLimit = ServicePointManager.DefaultConnectionLimit;
            if (installedPackageReferences.Count > defaultConnectionLimit)
            {
                ServicePointManager.DefaultConnectionLimit = Math.Min(10, installedPackageReferences.Count);
            }

            // The PackageSourceProvider reads from the underlying ISettings multiple times. One of the fields it reads is the password which is consequently decrypted
            // once for each package being installed. Per work item 2345, a couple of users are running into an issue where this results in an exception in native 
            // code. Instead, we'll use a cached set of sources. This should solve the issue and also give us some perf boost.
            SourceProvider = new CachedPackageSourceProvider(SourceProvider);

            var satellitePackages = new ConcurrentQueue<JObject>();
            if (DisableParallelProcessing)
            {
                foreach (var package in installedPackageReferences)
                {
                    RestorePackage(fileSystem, package.Identity, packageRestoreConsent, satellitePackages);
                }

                InstallSatellitePackages(fileSystem, satellitePackages);

                return true;
            }

            var tasks = installedPackageReferences.Select(installedPackageReference =>
                            Task.Factory.StartNew(() => RestorePackage(fileSystem, installedPackageReference.Identity, packageRestoreConsent, satellitePackages))).ToArray();

            Task.WaitAll(tasks);
            // Return true if we installed any satellite packages or if any of our install tasks succeeded.
            // TODO: Satellite packages
            InstallSatellitePackages(fileSystem, satellitePackages);

            return true;
        }

        private bool InstallSatellitePackages(IFileSystem packagesFolderFileSystem, ConcurrentQueue<JObject> satellitePackages)
        {
            if (satellitePackages.Count == 0)
            {
                return false;
            }

            var packageManager = CreatePackageManager(packagesFolderFileSystem, useSideBySidePaths: true);
            var filesystemInstallationTarget = new FilesystemInstallationTarget(packageManager);
            var packageActions = satellitePackages.Select(packageJSON => new NewPackageAction(NuGet.Client.PackageActionType.Download,
                    new PackageIdentity(packageJSON[Properties.PackageId].ToString(), new NuGetVersion (packageJSON[Properties.Version].ToString())), packageJSON, filesystemInstallationTarget, SourceRepository, null));

            // BUGBUG: See PackageExtractor.cs for locking mechanism used to handle concurrency
            NuGet.Client.Installation.ActionExecutor actionExecutor = new Client.Installation.ActionExecutor();
            actionExecutor.ExecuteActions(packageActions, Console);

            return true;
        }

        private void InstallPackages(IFileSystem packagesFolderFileSystem, ICollection<InstalledPackageReference> installedPackageReferences)
        {
            bool installedAny = ExecuteInParallel(packagesFolderFileSystem, installedPackageReferences);
            if (!installedAny && installedPackageReferences.Count > 0)
            {
                Console.WriteLine(LocalizedResourceManager.GetString("InstallCommandNothingToInstall"), Constants.PackageReferenceFile);
            }
        }

        private void RestorePackagesFromConfigFile(string packageReferenceFilePath, IFileSystem packagesFolderFileSystem)
        {
            if (FileSystem.FileExists(packageReferenceFilePath))
            {
                if (Console.Verbosity == NuGet.Verbosity.Detailed)
                {
                    Console.WriteLine(LocalizedResourceManager.GetString("RestoreCommandRestoringPackagesListedInFile"), packageReferenceFilePath);
                }

                InstallPackages(packagesFolderFileSystem, GetInstalledPackageReferences(packageReferenceFilePath, projectName: null));
            }
        }

        private void RestorePackagesForSolution(IFileSystem packagesFolderFileSystem, string solutionFileFullPath)
        {
            var solutionDirectory = Path.GetDirectoryName(solutionFileFullPath);

            // restore packages for the solution
            var solutionConfigFilePath = Path.Combine(
                solutionDirectory,
                NuGetConstants.NuGetSolutionSettingsFolder,
                Constants.PackageReferenceFile);
            RestorePackagesFromConfigFile(solutionConfigFilePath, packagesFolderFileSystem);

            // restore packages for projects
            var installedPackageReferences = GetInstalledPackageReferencesFromSolutionFile(solutionFileFullPath);
            InstallPackages(packagesFolderFileSystem, installedPackageReferences);
        }

        private ICollection<InstalledPackageReference> GetInstalledPackageReferencesFromSolutionFile(string solutionFileFullPath)
        {
            ISolutionParser solutionParser;
            if (EnvironmentUtility.IsMonoRuntime)
            {
                solutionParser = new XBuildSolutionParser();
            }
            else
            {
                solutionParser = new MSBuildSolutionParser();
            }

            var installedPackageReferences = new HashSet<InstalledPackageReference>();
            IEnumerable<string> projectFiles = Enumerable.Empty<string>();
            try
            {
                projectFiles = solutionParser.GetAllProjectFileNames(FileSystem, solutionFileFullPath);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                //if (ex.InnerException is InvalidProjectFileException)
                //{
                //    return GetPackageReferencesInDirectory(Path.GetDirectoryName(solutionFileFullPath));
                //}

                throw;
            }

            foreach (var projectFile in projectFiles)
            {
                if (!FileSystem.FileExists(projectFile))
                {
                    Console.WriteWarning(LocalizedResourceManager.GetString("RestoreCommandProjectNotFound"), projectFile);
                    continue;
                }

                string projectConfigFilePath = Path.Combine(
                    Path.GetDirectoryName(projectFile),
                    Constants.PackageReferenceFile);

                string projectName = Path.GetFileNameWithoutExtension(projectFile);

                installedPackageReferences.AddRange(GetInstalledPackageReferences(projectConfigFilePath, projectName));
            }

            return installedPackageReferences;
        }

        private static ICollection<InstalledPackageReference> GetInstalledPackageReferencesInDirectory(string directory)
        {
            var installedPackageReferences = new HashSet<InstalledPackageReference>();
            var configFiles = Directory.GetFiles(directory, "packages*.config", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f).StartsWith("packages.", StringComparison.OrdinalIgnoreCase));
            foreach (var configFile in configFiles)
            {   
                PackageReferenceFile file = new PackageReferenceFile(configFile);
                try
                {
                    installedPackageReferences.AddRange(CommandLineUtility.GetInstalledPackageReferences(file, requireVersion: true));
                }
                catch (InvalidOperationException)
                {
                    // Skip the file if it is not a valid xml file.
                }
            }

            return installedPackageReferences;
        }

        private static ICollection<InstalledPackageReference> GetInstalledPackageReferences(string fullConfigFilePath, string projectName)
        {
            var projectFileSystem = new PhysicalFileSystem(Path.GetDirectoryName(fullConfigFilePath));
            string configFileName = Path.GetFileName(fullConfigFilePath);

            PackageReferenceFile packageReferenceFile = new PackageReferenceFile(projectFileSystem, configFileName, projectName);
            return CommandLineUtility.GetInstalledPackageReferences(packageReferenceFile, requireVersion: true);
        }

        public override void ExecuteCommand()
        {
            CalculateEffectivePackageSaveMode();
            DetermineRestoreMode();
            if (_restoringForSolution && !String.IsNullOrEmpty(SolutionDirectory))
            {
                // option -SolutionDirectory is not valid when we are restoring packages for a solution
                throw new InvalidOperationException(LocalizedResourceManager.GetString("RestoreCommandOptionSolutionDirectoryIsInvalid"));
            }

            ReadSettings();
            string packagesFolder = GetPackagesFolder();
            IFileSystem packagesFolderFileSystem = CreateFileSystem(packagesFolder);

            if (_restoringForSolution)
            {
                RestorePackagesForSolution(packagesFolderFileSystem, _solutionFileFullPath);
            }
            else
            {
                // By default the PackageReferenceFile does not throw if the file does not exist at the specified path.
                // So we'll need to verify that the file exists.
                if (!FileSystem.FileExists(_packagesConfigFileFullPath))
                {
                    string message = String.Format(CultureInfo.CurrentCulture, LocalizedResourceManager.GetString("RestoreCommandFileNotFound"), _packagesConfigFileFullPath);
                    throw new InvalidOperationException(message);
                }

                Stopwatch watch = new Stopwatch();
                watch.Start();
                var installedPackageReferences = GetInstalledPackageReferences(_packagesConfigFileFullPath, projectName: null);
                watch.Stop();
                DisplayExecutedTime(watch.Elapsed, "GetInstalledPackageReferences");

                watch.Restart();
                InstallPackages(packagesFolderFileSystem, installedPackageReferences);
                watch.Stop();
                DisplayExecutedTime(watch.Elapsed, "RestoringPackages");
            }
        }
    }
}
