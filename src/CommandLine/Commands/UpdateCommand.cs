using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using NuGet.Common;

namespace NuGet.Commands
{
    [Command(typeof(NuGetResources), "update", "UpdateCommandDescription", UsageSummary = "<packages.config|solution>",
        UsageExampleResourceName = "UpdateCommandUsageExamples")]
    public class UpdateCommand : Command
    {
        private const string NuGetCommandLinePackageId = "NuGet.CommandLine";
        private const string NuGetExe = "NuGet.exe";
        private const string PackagesFolder = "packages";

        private readonly List<string> _sources = new List<string>();
        private readonly List<string> _ids = new List<string>();

        [ImportingConstructor]
        public UpdateCommand(IPackageRepositoryFactory repositoryFactory, IPackageSourceProvider sourceProvider)
        {
            RepositoryFactory = repositoryFactory;
            SourceProvider = sourceProvider;
        }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        public IPackageSourceProvider SourceProvider { get; private set; }

        [Option(typeof(NuGetResources), "UpdateCommandSourceDescription")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option(typeof(NuGetResources), "UpdateCommandIdDescription")]
        public ICollection<string> Id
        {
            get { return _ids; }
        }

        [Option(typeof(NuGetResources), "UpdateCommandRepositoryPathDescription")]
        public string RepositoryPath { get; set; }

        [Option(typeof(NuGetResources), "UpdateCommandSafeDescription")]
        public bool Safe { get; set; }

        [Option(typeof(NuGetResources), "UpdateCommandSelfDescription")]
        public bool Self { get; set; }

        [Option(typeof(NuGetResources), "UpdateCommandVerboseDescription")]
        public bool Verbose { get; set; }

        [Option(typeof(NuGetResources), "UpdateCommandPrerelease")]
        public bool Prerelease { get; set; }

        public override void ExecuteCommand()
        {
            if (Self)
            {
                Assembly assembly = typeof(UpdateCommand).Assembly;
                SelfUpdate(assembly.Location, new SemanticVersion(assembly.GetName().Version));
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
                    if (!File.Exists(inputFile))
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
            repositoryPath = repositoryPath ?? GetReposioryPath(project.Root);

            var pathResolver = new DefaultPackagePathResolver(repositoryPath);

            // Create the local and source repositories
            var localRepository = new PackageReferenceRepository(project, new SharedPackageRepository(repositoryPath));
            sourceRepository = sourceRepository ?? AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            IPackageConstraintProvider constraintProvider = localRepository;

            Console.WriteLine(NuGetResources.UpdatingProject, project.ProjectName);
            UpdatePackages(localRepository, sourceRepository, constraintProvider, pathResolver, project);
            project.Save();
        }

        private string GetReposioryPath(string projectRoot)
        {
            string packagesDir = RepositoryPath;

            if (String.IsNullOrEmpty(packagesDir))
            {
                // Try to resolve the packages directory from the project
                string projectDir = Path.GetDirectoryName(projectRoot);
                string solutionDir = ProjectHelper.GetSolutionDir(projectDir);

                return GetRepositoryPathFromSolution(solutionDir);
            }

            return GetPackagesDir(packagesDir);
        }

        private string GetRepositoryPathFromSolution(string solutionDir)
        {
            string packagesDir = RepositoryPath;

            if (String.IsNullOrEmpty(packagesDir) &&
                !String.IsNullOrEmpty(solutionDir))
            {
                packagesDir = Path.Combine(solutionDir, PackagesFolder);
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
                                     IPackageRepository sourceRepository,
                                     IPackageConstraintProvider constraintProvider,
                                     IPackagePathResolver pathResolver,
                                     IProjectSystem project)
        {
            var projectManager = new ProjectManager(sourceRepository, pathResolver, project, localRepository)
                                 {
                                     ConstraintProvider = constraintProvider
                                 };

            if (Verbose)
            {
                projectManager.Logger = Console;
            }

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

        internal void SelfUpdate(string exePath, SemanticVersion version)
        {
            Console.WriteLine(NuGetResources.UpdateCommandCheckingForUpdates, NuGetConstants.DefaultFeedUrl);

            // Get the nuget command line package from the specified repository
            IPackageRepository packageRepository = RepositoryFactory.CreateRepository(NuGetConstants.DefaultFeedUrl);

            IPackage package = packageRepository.FindPackage(NuGetCommandLinePackageId);

            // We didn't find it so complain
            if (package == null)
            {
                throw new CommandLineException(NuGetResources.UpdateCommandUnableToFindPackage, NuGetCommandLinePackageId);
            }

            Console.WriteLine(NuGetResources.UpdateCommandCurrentlyRunningNuGetExe, version);

            // Check to see if an update is needed
            if (version >= package.Version)
            {
                Console.WriteLine(NuGetResources.UpdateCommandNuGetUpToDate);
            }
            else
            {
                Console.WriteLine(NuGetResources.UpdateCommandUpdatingNuGet, package.Version);

                // Get NuGet.exe file from the package
                IPackageFile file = package.GetFiles().FirstOrDefault(f => Path.GetFileName(f.Path).Equals(NuGetExe, StringComparison.OrdinalIgnoreCase));

                // If for some reason this package doesn't have NuGet.exe then we don't want to use it
                if (file == null)
                {
                    throw new CommandLineException(NuGetResources.UpdateCommandUnableToLocateNuGetExe);
                }

                // Get the exe path and move it to a temp file (NuGet.exe.old) so we can replace the running exe with the bits we got 
                // from the package repository
                string renamedPath = exePath + ".old";
                Move(exePath, renamedPath);

                // Update the file
                UpdateFile(exePath, file);

                Console.WriteLine(NuGetResources.UpdateCommandUpdateSuccessful);
            }
        }

        protected virtual void UpdateFile(string exePath, IPackageFile file)
        {
            using (Stream fromStream = file.GetStream(), toStream = File.Create(exePath))
            {
                fromStream.CopyTo(toStream);
            }
        }

        protected virtual void Move(string oldPath, string newPath)
        {
            try
            {
                if (File.Exists(newPath))
                {
                    File.Delete(newPath);
                }
            }
            catch (FileNotFoundException)
            {

            }

            File.Move(oldPath, newPath);
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

        private class ProjectPair
        {
            public string PackagesConfigPath { get; set; }
            public IMSBuildProjectSystem Project { get; set; }
        }
    }
}