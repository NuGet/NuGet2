using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommand), "update", "UpdateCommandDescription", UsageSummary = "<packages.config|solution>",
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
        public FileConflictOption FileConflictAction { get; set; }

        public override void ExecuteCommand()
        {
            if (Self)
            {
                var selfUpdater = new SelfUpdater(RepositoryFactory) { Console = Console };
                selfUpdater.UpdateSelf();
            }
            else
            {
                string inputFile = GetInputFile();

                if (String.IsNullOrEmpty(inputFile))
                {
                    throw new CommandLineException(NuGetResources.InvalidFile);
                }
                else if (inputFile.EndsWith(Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase))
                {
                    UpdatePackages(inputFile);
                }
                else
                {
                    if (!FileSystem.FileExists(inputFile))
                    {
                        throw new CommandLineException(NuGetResources.UnableToFindSolution, inputFile);
                    }
                    else
                    {
                        string solutionDir = Path.GetDirectoryName(inputFile);
                        UpdateAllPackages(solutionDir);
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void UpdateAllPackages(string solutionDir)
        {
            Console.WriteLine(NuGetResources.ScanningForProjects);

            // Search recursively for all packages.config files
            var packagesConfigFiles = Directory.GetFiles(solutionDir, Constants.PackageReferenceFile, SearchOption.AllDirectories);
            var projects = packagesConfigFiles.Select(GetProject)
                                              .Where(p => p.Project != null)
                                              .ToList();

            if (projects.Count == 0)
            {
                Console.WriteLine(NuGetResources.NoProjectsFound);
                return;
            }

            if (projects.Count == 1)
            {
                Console.WriteLine(NuGetResources.FoundProject, projects.Single().Project.ProjectName);
            }
            else
            {
                Console.WriteLine(NuGetResources.FoundProjects, projects.Count, String.Join(", ", projects.Select(p => p.Project.ProjectName)));
            }

            string repositoryPath = GetRepositoryPathFromSolution(solutionDir);
            IPackageRepository sourceRepository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);

            foreach (var project in projects)
            {
                try
                {
                    UpdatePackages(project.PackagesConfigPath, project.Project, repositoryPath, sourceRepository);
                    if (Verbose)
                    {
                        Console.WriteLine();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteWarning(e.Message);
                }
            }
        }

        private static ProjectPair GetProject(string packagesConfigPath)
        {
            IMSBuildProjectSystem msBuildProjectSystem = null;
            try
            {
                msBuildProjectSystem = GetMSBuildProject(packagesConfigPath);
            }
            catch (CommandLineException)
            {

            }
            return new ProjectPair
            {
                PackagesConfigPath = packagesConfigPath,
                Project = msBuildProjectSystem
            };
        }

        private string GetInputFile()
        {
            if (Arguments.Any())
            {
                string path = Arguments[0];
                string extension = Path.GetExtension(path);

                if (extension.Equals(".config", StringComparison.OrdinalIgnoreCase))
                {
                    return GetPackagesConfigPath(path);
                }
                else if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
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

        private void UpdatePackages(string packagesConfigPath, IMSBuildProjectSystem project = null, string repositoryPath = null, IPackageRepository sourceRepository = null)
        {
            // Get the msbuild project
            project = project ?? GetMSBuildProject(packagesConfigPath);

            // Resolve the repository path
            repositoryPath = repositoryPath ?? GetRepositoryPath(project.Root);

            var sharedRepositoryFileSystem = new PhysicalFileSystem(repositoryPath);
            var pathResolver = new DefaultPackagePathResolver(sharedRepositoryFileSystem);

            // Create the local and source repositories
            var sharedPackageRepository = new SharedPackageRepository(pathResolver, sharedRepositoryFileSystem, sharedRepositoryFileSystem);
            var localRepository = new PackageReferenceRepository(project, sharedPackageRepository);
            sourceRepository = sourceRepository ?? AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);

            Console.WriteLine(NuGetResources.UpdatingProject, project.ProjectName);
            UpdatePackages(localRepository, sharedRepositoryFileSystem, sharedPackageRepository, sourceRepository, localRepository, pathResolver, project);
            project.Save();
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
                    Console.WriteLine(NuGetResources.LookingForInstalledPackages, relativePath);
                    return packagesDir;
                }
            }

            throw new CommandLineException(NuGetResources.UnableToLocatePackagesFolder);
        }

        private static IMSBuildProjectSystem GetMSBuildProject(string packageReferenceFilePath)
        {
            // Try to locate the project file associated with this packages.config file
            string directory = Path.GetDirectoryName(packageReferenceFilePath);
            string projectFile;
            if (ProjectHelper.TryGetProjectFile(directory, out projectFile))
            {
                return new MSBuildProjectSystem(projectFile);
            }

            throw new CommandLineException(NuGetResources.UnableToLocateProjectFile, packageReferenceFilePath);
        }

        internal void UpdatePackages(IPackageRepository localRepository,
                                     IFileSystem sharedRepositoryFileSystem,
                                     ISharedPackageRepository sharedPackageRepository,
                                     IPackageRepository sourceRepository,
                                     IPackageConstraintProvider constraintProvider,
                                     IPackagePathResolver pathResolver,
                                     IProjectSystem project)
        {
            var packageManager = new PackageManager(sourceRepository, pathResolver, sharedRepositoryFileSystem, sharedPackageRepository);

            var projectManager = new ProjectManager(sourceRepository, pathResolver, project, localRepository)
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

            projectManager.Logger = project.Logger = this;

            using (sourceRepository.StartOperation(RepositoryOperationNames.Update))
            {
                foreach (var package in GetPackages(localRepository))
                {
                    if (localRepository.Exists(package.Id))
                    {
                        try
                        {
                            // If the user explicitly allows prerelease or if the package being updated is prerelease we'll include prerelease versions in our list of packages
                            // being considered for an update.
                            bool allowPrerelease = Prerelease || !package.IsReleaseVersion();
                            if (Safe)
                            {
                                IVersionSpec safeRange = VersionUtility.GetSafeRange(package.Version);
                                projectManager.UpdatePackageReference(package.Id, safeRange, updateDependencies: true, allowPrereleaseVersions: allowPrerelease);
                            }
                            else
                            {
                                projectManager.UpdatePackageReference(package.Id, version: null, updateDependencies: true, allowPrereleaseVersions: allowPrerelease);
                            }
                        }
                        catch (InvalidOperationException e)
                        {
                            Console.WriteWarning(e.Message);
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
                    throw new CommandLineException(NuGetResources.UnableToFindPackages, String.Join(", ", invalid));
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
            if (FileConflictAction == FileConflictOption.Overwrite || _overwriteAll)
            {
                return FileConflictResolution.Overwrite;
            }

            // the -FileConflictAction is set to Ignore or user has chosen Ignore All previously
            if (FileConflictAction == FileConflictOption.Ignore || _ignoreAll)
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

        private class ProjectPair
        {
            public string PackagesConfigPath { get; set; }
            public IMSBuildProjectSystem Project { get; set; }
        }
    }
}