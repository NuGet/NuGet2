using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "update", "UpdateCommandDescription", UsageSummary = "<packages.config>")]
    public class UpdateCommand : Command {
        private const string DefaultFeedUrl = "https://go.microsoft.com/fwlink/?LinkID=206669";
        private const string NuGetCommandLinePackageId = "NuGet.CommandLine";
        private const string NuGetExe = "NuGet.exe";
        private const string PackagesFolder = "packages";

        private readonly List<string> _sources = new List<string>();
        private readonly List<string> _ids = new List<string>();

        [ImportingConstructor]
        public UpdateCommand(IPackageRepositoryFactory repositoryFactory, IPackageSourceProvider sourceProvider) {
            RepositoryFactory = repositoryFactory;
            SourceProvider = sourceProvider;
        }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        public IPackageSourceProvider SourceProvider { get; private set; }

        [Option(typeof(NuGetResources), "UpdateCommandSourceDescription")]
        public List<string> Source {
            get { return _sources; }
        }

        [Option(typeof(NuGetResources), "UpdateCommandIdDescription")]
        public List<string> Id {
            get { return _ids; }
        }

        [Option(typeof(NuGetResources), "UpdateCommandRepositoryPathDescription")]
        public string RepositoryPath { get; set; }

        [Option(typeof(NuGetResources), "UpdateCommandSafeDescription")]
        public bool Safe { get; set; }

        [Option(typeof(NuGetResources), "UpdateCommandSelfDescription")]
        public bool Self { get; set; }

        public override void ExecuteCommand() {
            if (Self) {
                Assembly assembly = typeof(UpdateCommand).Assembly;
                SelfUpdate(assembly.Location, assembly.GetName().Version);
            }
            else {
                string packagesFile = GetPackagesFile();

                if (String.IsNullOrEmpty(packagesFile)) {
                    throw new CommandLineException(NuGetResources.NoPackagesConfigSpecified);
                }
                else {
                    UpdatePackages(packagesFile);
                }
            }
        }

        private string GetPackagesFile() {
            if (Arguments.Any()) {
                return GetPackagesConfigPath(Arguments[0]);
            }

            return null;
        }

        private static string GetPackagesConfigPath(string path) {
            if (path.EndsWith(PackageReferenceRepository.PackageReferenceFile, StringComparison.OrdinalIgnoreCase)) {
                return Path.GetFullPath(path);
            }

            throw new CommandLineException(NuGetResources.NoPackagesConfigSpecified);
        }

        private void UpdatePackages(string packageReferenceFilePath) {
            // Get the reference file
            var referenceFile = new PackageReferenceFile(packageReferenceFilePath);

            // Get the msbuild project
            IMSBuildProjectSystem project = GetMSBuildProject(packageReferenceFilePath);

            // Resolve the repository path
            string repositoryPath = GetReposioryPath(project.Root);

            var pathResolver = new DefaultPackagePathResolver(repositoryPath);

            // Create the local and source repositories
            var localRepository = new LocalPackageRepository(repositoryPath);
            var sourceRepository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);

            UpdatePackages(referenceFile, project, localRepository, sourceRepository, pathResolver);
        }

        internal void UpdatePackages(PackageReferenceFile referenceFile,
                                     IMSBuildProjectSystem project,
                                     IPackageRepository localRepository,
                                     IPackageRepository sourceRepository,
                                     IPackagePathResolver pathResolver) {
            // Get the list of update operations
            var updateOperations = GetUpdates(referenceFile, localRepository, sourceRepository).ToList();

            // If there's nothing to update, bail out
            if (!updateOperations.Any()) {
                return;
            }

            foreach (var operation in updateOperations) {
                if (operation.Action == PackageAction.Uninstall) {
                    Console.WriteLine(NuGetResources.RemovingPackageReference, operation.Package.GetFullName());
                    referenceFile.DeleteEntry(operation.Package.Id, operation.Package.Version);
                }
                else {
                    Console.WriteLine(NuGetResources.AddingPackageReference, operation.Package.GetFullName());
                    referenceFile.AddEntry(operation.Package.Id, operation.Package.Version);
                }

                // Get the install path to this package
                string installPath = pathResolver.GetInstallPath(operation.Package);

                // Resolve all assemblies for the target framework
                var assembies = project.GetCompatibleItems(operation.Package.AssemblyReferences, "Assemblies");

                foreach (var assemblyReference in assembies) {
                    if (operation.Action == PackageAction.Uninstall) {
                        Console.WriteLine(NuGetResources.RemovingAssemblyReference, assemblyReference.Name);

                        project.RemoveReference(assemblyReference.Name);
                    }
                    else {
                        Console.WriteLine(NuGetResources.AddingAssemblyReference, assemblyReference.Name);

                        string referencePath = Path.Combine(installPath, assemblyReference.Path);

                        using (Stream stream = assemblyReference.GetStream()) {
                            project.AddReference(referencePath, stream);
                        }
                    }
                }
            }

            // Save the project
            project.Save();
        }

        private string GetReposioryPath(string projectRoot) {
            string packagesDir = RepositoryPath;

            if (String.IsNullOrEmpty(packagesDir)) {
                // Try to resolve the packages directory from the project
                string projectDir = Path.GetDirectoryName(projectRoot);
                string solutionDir = ProjectHelper.GetSolutionDir(projectDir);

                if (!String.IsNullOrEmpty(solutionDir)) {
                    packagesDir = Path.Combine(solutionDir, PackagesFolder);
                }
            }

            if (!String.IsNullOrEmpty(packagesDir)) {
                // Get the full path to the packages directory
                packagesDir = Path.GetFullPath(packagesDir);

                // REVIEW: Do we need to check for existence?
                if (Directory.Exists(packagesDir)) {
                    return packagesDir;
                }
            }

            throw new CommandLineException(NuGetResources.UnableToLocatePackagesFolder);
        }

        private IMSBuildProjectSystem GetMSBuildProject(string packageReferenceFilePath) {
            // Try to locate the project file associated with this packages.config file
            string directory = Path.GetDirectoryName(packageReferenceFilePath);
            string projectFile;
            if (ProjectHelper.TryGetProjectFile(directory, out projectFile)) {
                return new MSBuildProjectSystem(projectFile);
            }

            throw new CommandLineException(NuGetResources.UnableToLocateProjectFile);
        }

        internal IEnumerable<PackageOperation> GetUpdates(PackageReferenceFile referenceFile,
                                                          IPackageRepository localRepository,
                                                          IPackageRepository sourceRepository) {
            var candidates = new List<IPackage>();
            var constraints = new Dictionary<IPackage, IVersionSpec>();

            var packageReferences = GetPackageReferences(referenceFile);

            foreach (var packageReference in packageReferences) {
                // Look for the package in the local repository
                IPackage package = localRepository.FindPackage(packageReference.Id, packageReference.Version);

                // If we didn't find the package then we can't successfully perform the update
                if (package == null) {
                    Console.WriteWarning(NuGetResources.PackageDoesNotExist, packageReference.Id, packageReference.Version);
                    continue;
                }

                // Only consider packages that are valid to update (i.e. have binaries)
                if (!SupportsUpdates(package)) {
                    Console.WriteWarning(NuGetResources.SkippingUpdateCheck, packageReference.Id, packageReference.Version);
                    continue;
                }

                // Add it to the list of candidates
                candidates.Add(package);

                // Keep track of the constraints for this package
                constraints[package] = packageReference.VersionConstraint;
            }

            foreach (var package in GetUpdateOrder(candidates)) {
                IVersionSpec versionConstraint = constraints[package];

                // Use the right version constraint
                IVersionSpec constraint = Safe ? VersionUtility.GetSafeRange(package.Version) : versionConstraint;

                IPackage newPackage;

                if (constraint != null) {
                    newPackage = sourceRepository.FindPackage(package.Id, constraint);
                }
                else {
                    newPackage = sourceRepository.FindPackage(package.Id);
                }

                if (newPackage != null && newPackage.Version > package.Version) {
                    Console.WriteLine(NuGetResources.UpdatingPackage, newPackage.Id, newPackage.Version);

                    // Create the walker and resolve dependencies
                    var walker = new UpdateWalker(localRepository,
                                                  sourceRepository,
                                                  new DependentsWalker(localRepository),
                                                  Console,
                                                  updateDependencies: true);

                    // Return the list of operations we're going to perform
                    foreach (PackageOperation operation in walker.ResolveOperations(newPackage)) {
                        yield return operation;
                    }
                }
                else {
                    Console.WriteLine(NuGetResources.NoUpdatesAvailable, package.Id);
                }
            }
        }

        private IEnumerable<PackageReference> GetPackageReferences(PackageReferenceFile referenceFile) {
            IEnumerable<PackageReference> references = referenceFile.GetPackageReferences();
            if (Id.Any()) {
                var referenceIdSet = new HashSet<string>(references.Select(r => r.Id), StringComparer.OrdinalIgnoreCase);
                var idSet = new HashSet<string>(Id, StringComparer.OrdinalIgnoreCase);
                var invalid = Id.Where(id => !referenceIdSet.Contains(id));

                if (invalid.Any()) {
                    throw new CommandLineException(NuGetResources.UnableToFindPackages, String.Join(", ", invalid));
                }

                references = references.Where(r => idSet.Contains(r.Id));
            }
            return references;
        }

        internal void SelfUpdate(string exePath, Version version) {
            Console.WriteLine(NuGetResources.UpdateCommandCheckingForUpdates, DefaultFeedUrl);

            // Get the nuget command line package from the specified repository
            IPackageRepository packageRepository = RepositoryFactory.CreateRepository(DefaultFeedUrl);

            IPackage package = packageRepository.FindPackage(NuGetCommandLinePackageId);

            // We didn't find it so complain
            if (package == null) {
                throw new CommandLineException(NuGetResources.UpdateCommandUnableToFindPackage, NuGetCommandLinePackageId);
            }

            Console.WriteLine(NuGetResources.UpdateCommandCurrentlyRunningNuGetExe, version);

            // Check to see if an update is needed
            if (version >= package.Version) {
                Console.WriteLine(NuGetResources.UpdateCommandNuGetUpToDate);
            }
            else {
                Console.WriteLine(NuGetResources.UpdateCommandUpdatingNuGet, package.Version);

                // Get NuGet.exe file from the package
                IPackageFile file = package.GetFiles().FirstOrDefault(f => Path.GetFileName(f.Path).Equals(NuGetExe, StringComparison.OrdinalIgnoreCase));

                // If for some reason this package doesn't have NuGet.exe then we don't want to use it
                if (file == null) {
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

        protected virtual void UpdateFile(string exePath, IPackageFile file) {
            using (Stream fromStream = file.GetStream(), toStream = File.Create(exePath)) {
                fromStream.CopyTo(toStream);
            }
        }

        protected virtual void Move(string oldPath, string newPath) {
            try {
                if (File.Exists(newPath)) {
                    File.Delete(newPath);
                }
            }
            catch (FileNotFoundException) {

            }

            File.Move(oldPath, newPath);
        }

        private static bool SupportsUpdates(IPackage package) {
            // We only support assemblies for now
            return !package.GetContentFiles().Any() && package.AssemblyReferences.Any();
        }

        private IEnumerable<IPackage> GetUpdateOrder(IEnumerable<IPackage> packages) {
            var packageSorter = new PackageSorter();
            return packageSorter.GetPackagesByDependencyOrder(new ReadOnlyPackageRepository(packages)).Reverse();
        }
    }
}