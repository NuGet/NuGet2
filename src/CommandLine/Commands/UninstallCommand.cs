using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Common;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommand), "uninstall", "UninstallCommandDescription",
        MinArgs = 1, MaxArgs = 1, UsageSummaryResourceName = "UninstallCommandUsageSummary",
        UsageDescriptionResourceName = "UninstallCommandUsageDescription",
        UsageExampleResourceName = "UninstallCommandUsageExamples")]
    public class UninstallCommand : Command
    {
        [Option(typeof(NuGetCommand), "UninstallCommandProject")]
        public string Project { get; set; }

        [Option(typeof(NuGetCommand), "UninstallCommandSolutionDirectory")]
        public string SolutionDirectory { get; set; }

        [Option(typeof(NuGetCommand), "InstallCommandExcludeVersionDescription", AltName = "x")]
        public bool ExcludeVersion { get; set; }

        private string _projectFile;

        private bool AllowMultipleVersions
        {
            get { return !ExcludeVersion; }
        }

        [ImportingConstructor]
        public UninstallCommand()
        {
        }

        public override void ExecuteCommand()
        {
            string packageId = Arguments[0];
            
            LocateProjectFile();
            CalculateSolutionDirectory();
            Console.WriteLine("Solution directory is {0}", SolutionDirectory);

            string installPath = Path.Combine(SolutionDirectory, "packages");
            IFileSystem fileSystem = CreateFileSystem(installPath);
            var packageManager = CreatePackageManager(fileSystem);
            SemanticVersion version = null;

            using (packageManager.SourceRepository.StartOperation(
                RepositoryOperationNames.Install,
                packageId,
                version == null ? null : version.ToString()))
            {
                packageManager.UninstallPackage(packageId, version, forceRemove: false, removeDependencies: true);
            }
        }

        private void LocateProjectFile()
        {
            if (FileSystem.FileExists(Project))
            {
                _projectFile = FileSystem.GetFullPath(Project);
                return;
            }

            if (FileSystem.DirectoryExists(Project))
            {
                var projects = GetProjectFilesInDirectory(Project);
                if (projects.Count == 1)
                {
                    _projectFile = FileSystem.GetFullPath(projects[0]);
                    Console.WriteLine("Project file is {0}", _projectFile);
                    return;
                }

                if (projects.Count > 1)
                {
                    throw new InvalidOperationException("Multiple project files found");
                }

                // count == 0
                throw new InvalidOperationException("No project file is found");
            }

            throw new InvalidOperationException("Invalid value of -Project");
        }

        private void CalculateSolutionDirectory()
        {
            var dir = Path.GetDirectoryName(_projectFile);
            if (SolutionFileExists(dir))
            {
                return;
            }

            dir = Path.GetDirectoryName(dir);
            if (dir != null)
            {
                if (SolutionFileExists(dir))
                {
                    return;
                }
            }

            SolutionDirectory = Path.GetDirectoryName(_projectFile);
        }

        // Returns true if there exists exactly one solution file
        // containing _projectFile, in directory 'dir'. 
        private bool SolutionFileExists(string dir)
        {
            var solutionFiles = FileSystem.GetFiles(dir, "*.sln")
                .Where(s => SolutionFileContainsProject(s, _projectFile))
                .ToList();
            if (solutionFiles.Count == 1)
            {
                var solutionFile = FileSystem.GetFullPath(solutionFiles[0]);
                SolutionDirectory = Path.GetDirectoryName(solutionFile);
                return true;
            }

            return false;
        }

        private bool SolutionFileContainsProject(string solutionFile, string projectFile)
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

            return solutionParser.GetAllProjectFileNames(FileSystem, solutionFile).Any(
                s => String.Equals(s, projectFile, StringComparison.OrdinalIgnoreCase));
        }

        private IList<string> GetProjectFilesInDirectory(string directory)
        {
            var projectFiles = new List<string>();
            foreach (var file in FileSystem.GetFiles(directory, "*.*"))
            {
                var ext = Path.GetExtension(file);
                if (MSBuildProjectSystem.KnownProjectExtensions.Contains(ext))
                {
                    projectFiles.Add(file);
                }
            }

            return projectFiles;
        }        

        protected internal virtual IFileSystem CreateFileSystem(string path)
        {
            path = Path.GetFullPath(path);
            return new PhysicalFileSystem(path);
        }

        protected virtual IPackageManager CreatePackageManager(IFileSystem fileSystem)
        {
            var repository = CreateRepository();
            var pathResolver = new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: AllowMultipleVersions);

            IPackageRepository localRepository = new LocalPackageRepository(pathResolver, fileSystem);

            if (string.IsNullOrEmpty(Project))
            {
                var packageManager = new PackageManager(repository, pathResolver, fileSystem, localRepository)
                {
                    Logger = Console
                };
                return packageManager;
            }
            else
            {
                Project = Path.GetFullPath(Project);
                SolutionDirectory = Path.GetFullPath(SolutionDirectory);

                var packageManager = new MSBuildPackageManager(
                    repository,
                    pathResolver,
                    fileSystem,
                    localRepository,
                    Project,
                    SolutionDirectory)
                {
                    Logger = Console
                };
                return packageManager;
            }
        }

        protected IPackageRepository CreateRepository()
        {
            AggregateRepository repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, new string[] {});
            repository.Logger = Console;

            return repository;
        }
    }
}
