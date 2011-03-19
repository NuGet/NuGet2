using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Xml.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using NuGet.Common;

namespace NuGet.Commands {
    internal class ProjectPackageBuilder {
        private readonly Project _project;
        private FrameworkName _frameworkName;

        // Files we want to always exclude from the resulting package
        private static readonly HashSet<string> _excludeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            PackageReferenceRepository.PackageReferenceFile,
            "Web.Debug.config",
            "Web.Release.config"
        };

        // Packaging folders
        private const string ContentFolder = "content";
        private const string ReferenceFolder = "lib";
        private const string ToolsFolder = "tools";
        private const string SourcesFolder = "src";

        // Common item types
        private const string SourcesItemType = "Compile";
        private const string ContentItemType = "Content";
        private const string NuGetConfig = "nuget.config";
        private const string PackagesFolder = "packages";

        public ProjectPackageBuilder(string path, IConsole console) {
            _project = new Project(path);
            Console = console;
        }

        private string TargetPath {
            get;
            set;
        }

        private string OutputPath {
            get {
                if (TargetPath == null) {
                    return null;
                }
                return Path.GetDirectoryName(TargetPath.Substring(_project.DirectoryPath.Length).TrimEnd(Path.DirectorySeparatorChar));
            }
        }

        private FrameworkName TargetFramework {
            get {
                if (_frameworkName == null) {
                    // Get the target framework of the project
                    string targetFrameworkMoniker = _project.GetPropertyValue("TargetFrameworkMoniker");

                    if (!String.IsNullOrEmpty(targetFrameworkMoniker)) {
                        _frameworkName = new FrameworkName(targetFrameworkMoniker);
                    }
                }

                return _frameworkName;
            }
        }

        internal bool IncludeSources { get; set; }

        internal bool Debug { get; set; }

        private IConsole Console { get; set; }

        internal PackageBuilder BuildPackage() {
            if (TargetFramework != null) {
                Console.WriteLine(NuGetResources.BuildingProjectTargetingFramework, TargetFramework);
            }

            BuildProject();

            Console.WriteLine(NuGetResources.PackagingFilesFromOutputPath, OutputPath);

            var builder = new PackageBuilder();

            // If the package contains a nuspec file then use it for metadata
            ProcessNuspec(builder);

            try {
                // Populate the package builder with initial metadata from the assembly/exe
                AssemblyMetadataExtractor.ExtractMetadata(TargetPath, builder);
            }
            catch {
                Console.WriteWarning(NuGetResources.UnableToExtractAssemblyMetadata, Path.GetFileName(TargetPath));
                ExtractMetadataFromProject(builder);
            }

            bool anyNuspecFiles = builder.Files.Any();

            if (!anyNuspecFiles || IncludeSources) {
                // Add output files
                AddOutputFiles(builder);
            }

            if (!anyNuspecFiles) {
                // Add content files
                AddFiles(builder, ContentItemType, ContentFolder);
            }


            // Add sources if this is a symbol package
            if (IncludeSources) {
                AddFiles(builder, SourcesItemType, SourcesFolder);
            }


            if (!builder.Dependencies.Any()) {
                ProcessDependencies(builder);
            }

            // Set defaults if some required fields are missing
            if (String.IsNullOrEmpty(builder.Description)) {
                builder.Description = "Description";
                Console.WriteWarning(NuGetResources.Warning_UnspecifiedField, "Description", "Description");
            }

            if (!builder.Authors.Any()) {
                builder.Authors.Add("Author");
                Console.WriteWarning(NuGetResources.Warning_UnspecifiedField, "Author", "Author");
            }

            return builder;
        }

        private void BuildProject() {
            var properties = new Dictionary<string, string>();
            if (!Debug) {
                properties["Configuration"] = "Release";
            }
            else {
                properties["Configuration"] = "Debug";
            }

            var projectCollection = new ProjectCollection(ToolsetDefinitionLocations.Registry | ToolsetDefinitionLocations.ConfigurationFile);
            BuildRequestData requestData = new BuildRequestData(_project.FullPath, properties, _project.ToolsVersion, new string[0], null);
            BuildParameters parameters = new BuildParameters(projectCollection);
            parameters.Loggers = new[] { new ConsoleLogger() { Verbosity = LoggerVerbosity.Quiet } };
            parameters.NodeExeLocation = typeof(ProjectPackageBuilder).Assembly.Location;
            parameters.ToolsetDefinitionLocations = ToolsetDefinitionLocations.Registry | ToolsetDefinitionLocations.ConfigurationFile;
            BuildResult result = BuildManager.DefaultBuildManager.Build(parameters, requestData);
            // Build the project so that the outputs are created
            if (result.OverallResult == BuildResultCode.Failure) {
                // If the build fails, report the error
                throw new CommandLineException(NuGetResources.FailedToBuildProject, Path.GetFileName(_project.FullPath));
            }

            TargetResult targetResult;
            if (result.ResultsByTarget.TryGetValue("Build", out targetResult)) {
                if (targetResult.Items.Any()) {
                    TargetPath = targetResult.Items.First().ItemSpec;
                }
            }

            TargetPath = TargetPath ?? _project.GetPropertyValue("TargetPath");
        }

        private void ExtractMetadataFromProject(PackageBuilder builder) {
            builder.Id = builder.Id ??
                        _project.GetPropertyValue("AssemblyName") ??
                        Path.GetFileNameWithoutExtension(_project.FullPath);

            string version = _project.GetPropertyValue("Version");
            builder.Version = builder.Version ??
                              VersionUtility.ParseOptionalVersion(version) ??
                              new Version("1.0");
        }

        private void AddOutputFiles(PackageBuilder builder) {
            // Get the target framework of the project
            FrameworkName targetFramework = TargetFramework;

            // Get the target file path
            string targetPath = TargetPath;

            // List of extensions to allow in the output path
            var allowedOutputExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                ".dll",
                ".exe",
                ".xml"
            };

            if (IncludeSources) {
                // Include pdbs for symbol packages
                allowedOutputExtensions.Add(".pdb");
            }

            string projectOutputDirectory = Path.GetDirectoryName(targetPath);

            // By default we add all files in the project's output directory
            foreach (var file in Directory.GetFiles(projectOutputDirectory)) {
                string extension = Path.GetExtension(file).ToLowerInvariant();

                // Only look at files we care about
                if (!allowedOutputExtensions.Contains(extension)) {
                    continue;
                }

                string targetFilePath = null;

                if (targetFramework == null) {
                    targetFilePath = ReferenceFolder;
                }
                else {
                    targetFilePath = Path.Combine(ReferenceFolder, VersionUtility.GetFrameworkFolder(targetFramework));
                }

                builder.Files.Add(new PhysicalPackageFile {
                    SourcePath = file,
                    TargetPath = Path.Combine(targetFilePath, Path.GetFileName(file))
                });
            }
        }

        private void ProcessDependencies(PackageBuilder builder) {
            string packagesConfig = GetPackagesConfig();

            // No packages config then bail out
            if (String.IsNullOrEmpty(packagesConfig)) {
                return;
            }

            Console.WriteLine(NuGetResources.UsingPackagesConfigForDependencies);
            var file = new PackageReferenceFile(packagesConfig);

            // Try to find the package and remove all files we added from the output
            // that are part of packages
            IPackageRepository repository = GetPackagesRepository(_project.DirectoryPath);

            foreach (PackageReference reference in file.GetPackageReferences()) {
                IVersionSpec spec = VersionUtility.ParseVersionSpec(reference.Version.ToString());
                var dependency = new PackageDependency(reference.Id, spec);
                builder.Dependencies.Add(dependency);

                if (repository != null) {
                    IPackage package = repository.FindPackage(reference.Id, reference.Version);
                    if (package != null) {
                        IEnumerable<IPackageAssemblyReference> compatibleAssemblies;
                        if (VersionUtility.TryGetCompatibleItems(TargetFramework, package.AssemblyReferences, out compatibleAssemblies)) {
                            var assemblies = new HashSet<string>(compatibleAssemblies.Select(a => Path.GetFileNameWithoutExtension(a.Path)));
                            var filesToRemove = builder.Files.Where(f => assemblies.Contains(Path.GetFileNameWithoutExtension(f.Path))).ToList();

                            foreach (var item in filesToRemove) {
                                builder.Files.Remove(item);
                            }
                        }
                    }
                }
            }
        }

        private IPackageRepository GetPackagesRepository(string path) {
            // Only look 4 folders up to find the solution directory
            const int maxDepth = 4;
            int depth = 0;
            do {
                if (SolutionFileExists(path)) {
                    string target = Path.Combine(path, GetPackagesPath(path));
                    if (Directory.Exists(target)) {
                        return new LocalPackageRepository(target);
                    }
                }

                path = Path.GetDirectoryName(path);

                depth++;
            } while (depth < maxDepth);

            return null;
        }

        private bool SolutionFileExists(string path) {
            return Directory.GetFiles(path, "*.sln").Any();
        }

        private string GetPackagesPath(string dir) {
            string configPath = Path.Combine(dir, NuGetConfig);

            try {
                // Support the hidden feature
                if (File.Exists(configPath)) {
                    using (Stream stream = File.OpenRead(configPath)) {
                        return XDocument.Load(stream).Root.Element("repositoryPath").Value;
                    }
                }
            }
            catch (FileNotFoundException) {
            }

            return PackagesFolder;
        }

        private void ProcessNuspec(PackageBuilder builder) {
            string nuspecFile = GetNuspec();

            if (String.IsNullOrEmpty(nuspecFile)) {
                return;
            }

            using (Stream stream = File.OpenRead(nuspecFile)) {
                // Don't validate the manifest since this might be a partial manifest
                // The bulk of the metadata might be coming from the project.
                Manifest manifest = Manifest.ReadFrom(stream, validate: false);
                builder.Populate(manifest.Metadata);

                if (manifest.Files != null) {
                    string basePath = Path.GetDirectoryName(nuspecFile);
                    builder.PopulateFiles(basePath, manifest.Files);
                }
            }
        }

        private string GetNuspec() {
            return GetContentOrNone(file => Path.GetExtension(file).Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase));
        }

        private string GetPackagesConfig() {
            return GetContentOrNone(file => Path.GetFileName(file).Equals(PackageReferenceRepository.PackageReferenceFile, StringComparison.OrdinalIgnoreCase));
        }

        private string GetContentOrNone(Func<string, bool> matcher) {
            return GetFiles("Content").Concat(GetFiles("None")).FirstOrDefault(matcher);
        }

        private IEnumerable<string> GetFiles(string itemType) {
            return _project.GetItems(itemType).Select(item => Path.Combine(_project.DirectoryPath, item.UnevaluatedInclude));
        }

        private void AddFiles(PackageBuilder builder, string itemType, string targetFolder) {
            // Get the content files from the project
            foreach (var item in _project.GetItems(itemType)) {
                string file = Path.Combine(_project.DirectoryPath, item.UnevaluatedInclude);

                if (_excludeFiles.Contains(Path.GetFileName(file))) {
                    continue;
                }

                string targetFilePath = GetTargetPath(item);

                if (!File.Exists(file)) {
                    Console.WriteWarning(NuGetResources.Warning_FileDoesNotExist, targetFilePath);
                    continue;
                }

                builder.Files.Add(new PhysicalPackageFile {
                    SourcePath = file,
                    TargetPath = Path.Combine(targetFolder, targetFilePath)
                });
            }
        }

        private string GetTargetPath(ProjectItem item) {
            if (item.HasMetadata("Link")) {
                return item.GetMetadataValue("Link");
            }
            return item.UnevaluatedInclude;
        }
    }
}
