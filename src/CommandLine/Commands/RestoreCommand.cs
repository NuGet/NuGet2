using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NuGet.Common;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommand), "restore", "RestoreCommandDescription",
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

        [Option(typeof(NuGetCommand), "RestoreCommandRequireConsent")]
        public bool RequireConsent { get; set; }

        [Option(typeof(NuGetCommand), "RestoreCommandPackagesDirectory", AltName = "OutputDirectory")]
        public string PackagesDirectory { get; set; }

        [Option(typeof(NuGetCommand), "RestoreCommandSolutionDirectory")]
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
            var physicalFileSystem = new PhysicalFileSystem(path);
            physicalFileSystem.Logger = Console;
            return physicalFileSystem;
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

        // Do a very quick check of whether a package in installed by checking whether the nupkg file exists
        private static bool IsPackageInstalled(IPackageRepository repository, IFileSystem packagesFolderFileSystem, string packageId, SemanticVersion version)
        {
            if (version != null)
            {
                // If we know exactly what package to lookup, check if it's already installed locally. 
                // We'll do this by checking if the package directory exists on disk.
                var localRepository = (LocalPackageRepository)repository;
                var packagePaths = localRepository.GetPackageLookupPaths(packageId, version);
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
            string packageId,
            SemanticVersion version,
            bool packageRestoreConsent,
            ConcurrentQueue<IPackage> satellitePackages)
        {
            var packageManager = CreatePackageManager(packagesFolderFileSystem, useSideBySidePaths: true);
            if (IsPackageInstalled(packageManager.LocalRepository, packagesFolderFileSystem, packageId, version))
            {
                return false;
            }

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
                    satellitePackages.Enqueue(package);
                    return true;
                }

                // During package restore with parallel build, multiple projects would try to write to disk simultaneously which results in write contentions.
                // We work around this issue by ensuring only one instance of the exe installs the package.
                PackageExtractor.InstallPackage(packageManager, package);
                return true;
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

            var satellitePackages = new ConcurrentQueue<IPackage>();
            if (DisableParallelProcessing)
            {
                foreach (var package in packageReferences)
                {
                    RestorePackage(fileSystem, package.Id, package.Version, packageRestoreConsent, satellitePackages);
                }

                InstallSatellitePackages(fileSystem, satellitePackages);

                return true;
            }

            var tasks = packageReferences.Select(package =>
                            Task.Factory.StartNew(() => RestorePackage(fileSystem, package.Id, package.Version, packageRestoreConsent, satellitePackages))).ToArray();

            Task.WaitAll(tasks);
            // Return true if we installed any satellite packages or if any of our install tasks succeeded.
            return InstallSatellitePackages(fileSystem, satellitePackages) ||
                   tasks.All(p => !p.IsFaulted && p.Result);
        }

        private bool InstallSatellitePackages(IFileSystem packagesFolderFileSystem, ConcurrentQueue<IPackage> satellitePackages)
        {
            if (satellitePackages.Count == 0)
            {
                return false;
            }

            var packageManager = CreatePackageManager(packagesFolderFileSystem, useSideBySidePaths: true);
            foreach (var package in satellitePackages)
            {
                packageManager.InstallPackage(package, ignoreDependencies: true, allowPrereleaseVersions: false);
            }
            return true;
        }

        private void InstallPackages(IFileSystem packagesFolderFileSystem, ICollection<PackageReference> packageReferences)
        {
            bool installedAny = ExecuteInParallel(packagesFolderFileSystem, packageReferences);
            if (!installedAny && packageReferences.Count > 0)
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

                InstallPackages(packagesFolderFileSystem, GetPackageReferences(packageReferenceFilePath, projectName: null));
            }
        }

        private void RestorePackagesForSolution(IFileSystem packagesFolderFileSystem, string solutionFileFullPath)
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
            var solutionDirectory = Path.GetDirectoryName(solutionFileFullPath);

            // restore packages for the solution
            var solutionConfigFilePath = Path.Combine(
                solutionDirectory,
                NuGetConstants.NuGetSolutionSettingsFolder,
                Constants.PackageReferenceFile);
            RestorePackagesFromConfigFile(solutionConfigFilePath, packagesFolderFileSystem);

            // restore packages for projects
            var packageReferences = new HashSet<PackageReference>();
            foreach (var projectFile in solutionParser.GetAllProjectFileNames(FileSystem, solutionFileFullPath))
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

                packageReferences.AddRange(GetPackageReferences(projectConfigFilePath, projectName));
            }

            InstallPackages(packagesFolderFileSystem, packageReferences);
        }

        private ICollection<PackageReference> GetPackageReferences(string fullConfigFilePath, string projectName)
        {
            var projectFileSystem = new PhysicalFileSystem(Path.GetDirectoryName(fullConfigFilePath));
            projectFileSystem.Logger = Console;
            string configFileName = Path.GetFileName(fullConfigFilePath);

            PackageReferenceFile file = new PackageReferenceFile(projectFileSystem, configFileName, projectName);
            return CommandLineUtility.GetPackageReferences(file, requireVersion: true);
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

                InstallPackages(packagesFolderFileSystem, GetPackageReferences(_packagesConfigFileFullPath, projectName: null));
            }
        }
    }
}
