using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;
using NuGet.Resolver;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommand), "update", "UpdateCommandDescription", UsageSummary = "<packages.config|solution|project>",
        UsageExampleResourceName = "UpdateCommandUsageExamples")]
    public class UpdateCommand : Command, ILogger
    {
        private readonly List<string> _sources = new List<string>();
        private readonly List<string> _ids = new List<string>();
        private bool _overwriteAll, _ignoreAll;

        [Option(typeof(NuGetCommand), "UpdateCommandSourceDescription")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option(typeof(NuGetCommand), "UpdateCommandIdDescription")]
        public ICollection<string> Id
        {
            get { return _ids; }
        }

        [Option(typeof(NuGetCommand), "UpdateCommandRepositoryPathDescription")]
        public string RepositoryPath { get; set; }

        [Option(typeof(NuGetCommand), "UpdateCommandSafeDescription")]
        public bool Safe { get; set; }

        [Option(typeof(NuGetCommand), "UpdateCommandSelfDescription")]
        public bool Self { get; set; }

        [Option(typeof(NuGetCommand), "UpdateCommandVerboseDescription")]
        public bool Verbose { get; set; }

        [Option(typeof(NuGetCommand), "UpdateCommandPrerelease")]
        public bool Prerelease { get; set; }

        [Option(typeof(NuGetCommand), "UpdateCommandFileConflictAction")]
        public FileConflictAction FileConflictAction { get; set; }

        public override void ExecuteCommand()
        {
            // update with self as parameter
            if (Self)
            {
                var selfUpdater = new SelfUpdater(RepositoryFactory) 
                { 
                    Console = Console,
                    IncludePrerelease = Prerelease
                };
                selfUpdater.UpdateSelf();
                return;
            }

            string inputFile = GetInputFile();

            if (string.IsNullOrEmpty(inputFile))
            {
                throw new CommandLineException(NuGetResources.InvalidFile);
            }

            string inputFileName = Path.GetFileName(inputFile);
            // update with packages.config as parameter
            if (PackageReferenceFile.IsValidConfigFileName(inputFileName))
            {
                UpdatePackages(inputFile);
                return;
            }

            // update with project file as parameter
            if (ProjectHelper.SupportedProjectExtensions.Contains(Path.GetExtension(inputFile) ?? string.Empty))
            {
                if (!FileSystem.FileExists(inputFile))
                {
                    throw new CommandLineException(NuGetResources.UnableToFindProject, inputFile);
                }

                UpdatePackages(new MSBuildProjectSystem(inputFile));
                return;
            }
                
            if (!FileSystem.FileExists(inputFile))
            {
                throw new CommandLineException(NuGetResources.UnableToFindSolution, inputFile);
            }
            
            // update with solution as parameter
            string solutionDir = Path.GetDirectoryName(inputFile);
            UpdateAllPackages(solutionDir);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void UpdateAllPackages(string solutionDir)
        {
            Console.WriteLine(LocalizedResourceManager.GetString("ScanningForProjects"));

            // Search recursively for all packages.xxx.config files
            string[] packagesConfigFiles = Directory.GetFiles(
                solutionDir, "*.config", SearchOption.AllDirectories);

            var projectSystems = packagesConfigFiles.Where(s => Path.GetFileName(s).StartsWith("packages.", StringComparison.OrdinalIgnoreCase))
                                              .Select(GetProjectSystem)
                                              .Where(p => p != null)
                                              .Distinct()
                                              .ToList();

            if (projectSystems.Count == 0)
            {
                Console.WriteLine(LocalizedResourceManager.GetString("NoProjectsFound"));
                return;
            }

            if (projectSystems.Count == 1)
            {
                Console.WriteLine(LocalizedResourceManager.GetString("FoundProject"), projectSystems.Single().ProjectName);
            }
            else
            {
                Console.WriteLine(LocalizedResourceManager.GetString("FoundProjects"), projectSystems.Count, String.Join(", ", projectSystems.Select(p => p.ProjectName)));
            }

            string repositoryPath = GetRepositoryPath(solutionDir);
            IPackageRepository sourceRepository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);

            foreach (var projectSystem in projectSystems)
            {
                try
                {
                    UpdatePackages(projectSystem, repositoryPath, sourceRepository);
                    if (Verbose)
                    {
                        Console.WriteLine();
                    }
                }
                catch (Exception e)
                {
                    if (Console.Verbosity == NuGet.Verbosity.Detailed)
                    {
                        Console.WriteWarning(e.ToString());
                    }
                    else
                    {
                        Console.WriteWarning(e.Message);
                    }
                }
            }
        }

        private static IMSBuildProjectSystem GetProjectSystem(string path)
        {
            try
            {
                return GetMSBuildProjectSystem(path);
            }
            catch (CommandLineException)
            {

            }

            return  null;
        }

        private string GetInputFile()
        {
            if (Arguments.Any())
            {
                string path = Arguments[0];
                string extension = Path.GetExtension(path) ?? string.Empty;

                if (extension.Equals(".config", StringComparison.OrdinalIgnoreCase))
                {
                    return GetPackagesConfigPath(path);
                }
                
                if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFullPath(path);
                }
                
                if (ProjectHelper.SupportedProjectExtensions.Contains(extension))
                {
                    return Path.GetFullPath(path);
                }
            }

            return null;
        }

        private static string GetPackagesConfigPath(string path)
        {
            if (path.EndsWith(Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(path);
            }

            return null;
        }

        private void UpdatePackages(string packagesConfigPath)
        {
            var projectSystem =  GetMSBuildProjectSystem(packagesConfigPath);
            UpdatePackages(projectSystem);
        }

        private void UpdatePackages(IMSBuildProjectSystem projectSystem, string repositoryPath = null, IPackageRepository sourceRepository = null)
        {
            // Resolve the repository path
            repositoryPath = repositoryPath ?? GetRepositoryPath(projectSystem.Root);

            var sharedRepositoryFileSystem = new PhysicalFileSystem(repositoryPath);
            var pathResolver = new DefaultPackagePathResolver(sharedRepositoryFileSystem);

            // Create the local and source repositories
            var sharedPackageRepository = new SharedPackageRepository(pathResolver, sharedRepositoryFileSystem, sharedRepositoryFileSystem);
            var localRepository = new PackageReferenceRepository(projectSystem, projectSystem.ProjectName, sharedPackageRepository);
            sourceRepository = sourceRepository ?? AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);

            Console.WriteLine(LocalizedResourceManager.GetString("UpdatingProject"), projectSystem.ProjectName);
            UpdatePackages(localRepository, sharedRepositoryFileSystem, sharedPackageRepository, sourceRepository, localRepository, pathResolver, projectSystem);
            projectSystem.Save();
        }

        private string GetRepositoryPath(string projectRoot)
        {
            string packagesDir = RepositoryPath;

            if (String.IsNullOrEmpty(packagesDir))
            {
                packagesDir = Settings.GetRepositoryPath();
                if (String.IsNullOrEmpty(packagesDir))
                {
                    // Try to resolve the packages directory from the project
                    string projectDir = Path.GetDirectoryName(projectRoot);
                    string solutionDir = ProjectHelper.GetSolutionDir(projectDir);

                    return GetRepositoryPathFromSolution(solutionDir);
                }
            }

            return GetPackagesDir(packagesDir);
        }

        private string GetRepositoryPathFromSolution(string solutionDir)
        {
            string packagesDir = RepositoryPath;

            if (String.IsNullOrEmpty(packagesDir) &&
                !String.IsNullOrEmpty(solutionDir))
            {
                packagesDir = Path.Combine(solutionDir, CommandLineConstants.PackagesDirectoryName);
            }

            return GetPackagesDir(packagesDir);
        }

        private string GetPackagesDir(string packagesDir)
        {
            if (!String.IsNullOrEmpty(packagesDir))
            {
                // Get the full path to the packages directory
                packagesDir = Path.GetFullPath(packagesDir);

                // REVIEW: Do we need to check for existence?
                if (Directory.Exists(packagesDir))
                {
                    string currentDirectory = Directory.GetCurrentDirectory();
                    string relativePath = PathUtility.GetRelativePath(PathUtility.EnsureTrailingSlash(currentDirectory), packagesDir);
                    Console.WriteLine(LocalizedResourceManager.GetString("LookingForInstalledPackages"), relativePath);
                    return packagesDir;
                }
            }

            throw new CommandLineException(LocalizedResourceManager.GetString("UnableToLocatePackagesFolder"));
        }

        private static IMSBuildProjectSystem GetMSBuildProjectSystem(string packageReferenceFilePath)
        {
            // Try to locate the project file associated with this packages.config file
            var directory = Path.GetDirectoryName(packageReferenceFilePath);
            var projectFiles = ProjectHelper.GetProjectFiles(directory).Take(2).ToArray();
         
            if (projectFiles.Length == 0)
            {
                throw new CommandLineException(LocalizedResourceManager.GetString("UnableToLocateProjectFile"), packageReferenceFilePath);
            }

            if (projectFiles.Length > 1)
            {
                throw new CommandLineException(LocalizedResourceManager.GetString("MultipleProjectFilesFound"), packageReferenceFilePath);
            }

            return new MSBuildProjectSystem(projectFiles[0]);
        }

        internal void UpdatePackages(IPackageRepository localRepository,
                                     IFileSystem sharedRepositoryFileSystem,
                                     ISharedPackageRepository sharedPackageRepository,
                                     IPackageRepository sourceRepository,
                                     IPackageConstraintProvider constraintProvider,
                                     IPackagePathResolver pathResolver,
                                     IProjectSystem projectSystem)
        {
            var packageManager = new PackageManager(sourceRepository, pathResolver, sharedRepositoryFileSystem, sharedPackageRepository);

            var projectManager = new ProjectManager(packageManager, pathResolver, projectSystem, localRepository)
                                 {
                                     ConstraintProvider = constraintProvider
                                 };

            // Fix for work item 2411: When updating packages, we did not add packages to the shared package repository. 
            // Consequently, when querying the package reference repository, we would have package references with no backing package files in
            // the shared repository. This would cause the reference repository to skip the package assuming that the entry is invalid.
            projectManager.PackageReferenceAdded += (sender, eventArgs) =>
            {
                PackageExtractor.InstallPackage(packageManager, eventArgs.Package);
            };

            projectManager.Logger = projectSystem.Logger = this;

            foreach (var package in GetPackages(localRepository))
            {
                if (localRepository.Exists(package.Id))
                {
                    using (sourceRepository.StartOperation(RepositoryOperationNames.Update, package.Id, mainPackageVersion: null))
                    {
                        try
                        {
                            // If the user explicitly allows prerelease or if the package being updated is prerelease we'll include prerelease versions in our list of packages
                            // being considered for an update.
                            bool allowPrerelease = Prerelease || !package.IsReleaseVersion();
                            var resolver = new ActionResolver()
                            {
                                AllowPrereleaseVersions = allowPrerelease
                            };
                            var updateUtility = new UpdateUtility(resolver)
                            {
                                AllowPrereleaseVersions = allowPrerelease,
                                Safe = Safe
                            };

                            var operations = updateUtility.ResolveActionsForUpdate(package.Id, null, new[] { projectManager }, false);
                            var userOperationExecutor = new ActionExecutor();
                            userOperationExecutor.Execute(operations);
                        }
                        catch (InvalidOperationException e)
                        {
                            if (Console.Verbosity == NuGet.Verbosity.Detailed)
                            {
                                Console.WriteWarning(e.ToString());
                            }
                            else
                            {
                                Console.WriteWarning(e.Message);
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<IPackage> GetPackages(IPackageRepository repository)
        {
            var packages = repository.GetPackages();
            if (Id.Any())
            {
                var packageIdSet = new HashSet<string>(packages.Select(r => r.Id), StringComparer.OrdinalIgnoreCase);
                var idSet = new HashSet<string>(Id, StringComparer.OrdinalIgnoreCase);
                var invalid = Id.Where(id => !packageIdSet.Contains(id));

                if (invalid.Any())
                {
                    throw new CommandLineException(LocalizedResourceManager.GetString("UnableToFindPackages"), String.Join(", ", invalid));
                }

                packages = packages.Where(r => idSet.Contains(r.Id));
            }
            var packageSorter = new PackageSorter(targetFramework: null);
            return packageSorter.GetPackagesByDependencyOrder(new ReadOnlyPackageRepository(packages)).Reverse();
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            if (Verbose && Console != null)
            {
                Console.Log(level, message, args);
            }
        }

        public FileConflictResolution ResolveFileConflict(string message)
        {
            // the -FileConflictAction is set to Overwrite or user has chosen Overwrite All previously
            if (FileConflictAction == FileConflictAction.Overwrite || _overwriteAll)
            {
                return FileConflictResolution.Overwrite;
            }

            // the -FileConflictAction is set to Ignore or user has chosen Ignore All previously
            if (FileConflictAction == FileConflictAction.Ignore || _ignoreAll)
            {
                return FileConflictResolution.Ignore;
            }

            // otherwise, prompt user for choice, unless we're in non-interactive mode
            if (Console != null && !Console.IsNonInteractive)
            {
                var resolution = Console.ResolveFileConflict(message);
                _overwriteAll = (resolution == FileConflictResolution.OverwriteAll);
                _ignoreAll = (resolution == FileConflictResolution.IgnoreAll);
                return resolution;
            }

            return FileConflictResolution.Ignore;
        }
    }
}
