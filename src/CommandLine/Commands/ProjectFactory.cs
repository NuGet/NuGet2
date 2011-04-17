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
    internal class ProjectFactory : IPropertyProvider {
        private readonly Project _project;
        private FrameworkName _frameworkName;
        private ILogger _logger;

        // Files we want to always exclude from the resulting package
        private static readonly HashSet<string> _excludeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            PackageReferenceRepository.PackageReferenceFile,
            "Web.Debug.config",
            "Web.Release.config"
        };

        private readonly Dictionary<string, string> _properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
        private const string TransformFileExtension = ".transform";

        public ProjectFactory(string path)
            : this(new Project(path)) {
        }

        public ProjectFactory(Project project) {
            _project = project;
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private string TargetPath {
            get;
            set;
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

        public bool IncludeSymbols { get; set; }

        public Dictionary<string, string> Properties { get; private set; }

        public bool IsTool { get; set; }

        public ILogger Logger {
            get {
                return _logger ?? NullLogger.Instance;
            }
            set {
                _logger = value;
            }
        }

        public PackageBuilder CreateBuilder() {
            if (TargetFramework != null) {
                Logger.Log(MessageLevel.Info, NuGetResources.BuildingProjectTargetingFramework, TargetFramework);
            }

            BuildProject();

            Logger.Log(MessageLevel.Info, NuGetResources.PackagingFilesFromOutputPath, Path.GetDirectoryName(TargetPath));

            var builder = new PackageBuilder();

            try {
                // Populate the package builder with initial metadata from the assembly/exe
                AssemblyMetadataExtractor.ExtractMetadata(builder, TargetPath);
            }
            catch {
                Logger.Log(MessageLevel.Warning, NuGetResources.UnableToExtractAssemblyMetadata, Path.GetFileName(TargetPath));
                ExtractMetadataFromProject(builder);
            }

            // Set the properties that were resolved from the assembly/project so they can be 
            // resolved by name if the nuspec contains tokens
            _properties.Clear();
            _properties.Add("Id", builder.Id);
            _properties.Add("Version", builder.Version.ToString());

            if (!String.IsNullOrEmpty(builder.Description)) {
                _properties.Add("Description", builder.Description);
            }

            string projectAuthor = builder.Authors.FirstOrDefault();
            if (!String.IsNullOrEmpty(projectAuthor)) {
                _properties.Add("Author", projectAuthor);
            }

            // If the package contains a nuspec file then use it for metadata
            ProcessNuspec(builder);

            // Remove the extra author
            if (builder.Authors.Count > 1) {
                builder.Authors.Remove(projectAuthor);
            }

            builder.Version = VersionUtility.TrimVersion(builder.Version);

            // Add output files
            AddOutputFiles(builder);

            // Add content files
            AddFiles(builder, ContentItemType, ContentFolder);

            // Add sources if this is a symbol package
            if (IncludeSymbols) {
                AddFiles(builder, SourcesItemType, SourcesFolder);
            }

            ProcessDependencies(builder);

            // Set defaults if some required fields are missing
            if (String.IsNullOrEmpty(builder.Description)) {
                builder.Description = "Description";
                Logger.Log(MessageLevel.Warning, NuGetResources.Warning_UnspecifiedField, "Description", "Description");
            }

            if (!builder.Authors.Any()) {
                builder.Authors.Add(Environment.UserName);
                Logger.Log(MessageLevel.Warning, NuGetResources.Warning_UnspecifiedField, "Author", Environment.UserName);
            }

            return builder;
        }

        dynamic IPropertyProvider.GetPropertyValue(string propertyName) {
            string value;
            if (!_properties.TryGetValue(propertyName, out value)) {
                ProjectProperty property = _project.GetProperty(propertyName);
                if (property != null) {
                    value = property.EvaluatedValue;
                }
            }

            return value;
        }

        private void BuildProject() {
            var projectCollection = new ProjectCollection(ToolsetDefinitionLocations.Registry | ToolsetDefinitionLocations.ConfigurationFile);
            BuildRequestData requestData = new BuildRequestData(_project.FullPath, Properties, _project.ToolsVersion, new string[0], null);
            BuildParameters parameters = new BuildParameters(projectCollection);
            parameters.Loggers = new[] { new ConsoleLogger() { Verbosity = LoggerVerbosity.Quiet } };
            parameters.NodeExeLocation = typeof(ProjectFactory).Assembly.Location;
            parameters.ToolsetDefinitionLocations = projectCollection.ToolsetLocations;
            BuildResult result = BuildManager.DefaultBuildManager.Build(parameters, requestData);
            // Build the project so that the outputs are created
            if (result.OverallResult == BuildResultCode.Failure) {
                // If the build fails, report the error
                throw new CommandLineException(NuGetResources.FailedToBuildProject, Path.GetFileName(_project.FullPath));
            }

            TargetPath = ResolveTargetPath(result);
        }

        private string ResolveTargetPath(BuildResult result) {
            string targetPath = null;
            TargetResult targetResult;
            if (result.ResultsByTarget.TryGetValue("Build", out targetResult)) {
                if (targetResult.Items.Any()) {
                    targetPath = targetResult.Items.First().ItemSpec;
                }
            }

            return targetPath ?? _project.GetPropertyValue("TargetPath");
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

            if (IncludeSymbols) {
                // Include pdbs for symbol packages
                allowedOutputExtensions.Add(".pdb");
            }

            string projectOutputDirectory = Path.GetDirectoryName(targetPath);

            string targetFileName = Path.GetFileNameWithoutExtension(targetPath);

            // By default we add all files in the project's output directory
            foreach (var file in Directory.GetFiles(projectOutputDirectory, targetFileName + "*")) {
                string extension = Path.GetExtension(file).ToLowerInvariant();

                // Only look at files we care about
                if (!allowedOutputExtensions.Contains(extension)) {
                    continue;
                }

                string targetFolder = null;

                if (IsTool) {
                    targetFolder = ToolsFolder;
                }
                else {
                    if (targetFramework == null) {
                        targetFolder = ReferenceFolder;
                    }
                    else {
                        targetFolder = Path.Combine(ReferenceFolder, VersionUtility.GetFrameworkFolder(targetFramework));
                    }
                }

                builder.Files.Add(new PhysicalPackageFile {
                    SourcePath = file,
                    TargetPath = Path.Combine(targetFolder, Path.GetFileName(file))
                });
            }
        }

        private void ProcessDependencies(PackageBuilder builder) {
            string packagesConfig = GetPackagesConfig();

            // No packages config then bail out
            if (String.IsNullOrEmpty(packagesConfig)) {
                return;
            }

            Logger.Log(MessageLevel.Info, NuGetResources.UsingPackagesConfigForDependencies);

            var file = new PackageReferenceFile(packagesConfig);

            // Get the solution repository
            IPackageRepository repository = GetPackagesRepository(_project.DirectoryPath);

            // Collect all packages
            var packages = new List<IPackage>();
            foreach (PackageReference reference in file.GetPackageReferences()) {
                if (repository != null) {
                    IPackage package = repository.FindPackage(reference.Id, reference.Version);
                    if (package != null) {
                        packages.Add(package);
                    }
                }
            }

            // Add the transform file to the package builder
            ProcessTransformFiles(builder, packages.SelectMany(GetTransformFiles));

            // Reduce the set of packages we want to include as dependencies to the minimal set.
            // Normally, packages.config has the full closure included, we to only add top level
            // packages, i.e packages with in-degree 0
            foreach (var package in GetMinimumSet(packages)) {
                // Don't add duplicate dependencies
                if (builder.Dependencies.Any(d => d.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase))) {
                    continue;
                }

                IVersionSpec spec = VersionUtility.ParseVersionSpec(package.Version.ToString());
                var dependency = new PackageDependency(package.Id, spec);
                builder.Dependencies.Add(dependency);
            }
        }

        private IEnumerable<IPackage> GetMinimumSet(List<IPackage> packages) {
            return new Walker(packages).GetMinimalSet();
        }

        private void ProcessTransformFiles(PackageBuilder builder, IEnumerable<IPackageFile> transformFiles) {
            // Group transform by target file
            var transformGroups = transformFiles.GroupBy(file => RemoveExtension(file.Path), StringComparer.OrdinalIgnoreCase);
            var fileLookup = builder.Files.ToDictionary(file => file.Path, StringComparer.OrdinalIgnoreCase);

            foreach (var tranfromGroup in transformGroups) {
                IPackageFile file;
                if (fileLookup.TryGetValue(tranfromGroup.Key, out file)) {
                    // Replace the original file with a file that removes the transforms
                    builder.Files.Remove(file);
                    builder.Files.Add(new ReverseTransformFormFile(file, tranfromGroup));
                }
            }
        }

        /// <summary>
        /// Removes a file extension keeping the full path intact
        /// </summary>
        private static string RemoveExtension(string path) {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
        }

        private IEnumerable<IPackageFile> GetTransformFiles(IPackage package) {
            return package.GetContentFiles().Where(IsTransformFile);
        }

        private static bool IsTransformFile(IPackageFile file) {
            return Path.GetExtension(file.Path).Equals(TransformFileExtension, StringComparison.OrdinalIgnoreCase);
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

            Logger.Log(MessageLevel.Info, NuGetResources.UsingNuspecForMetadata, Path.GetFileName(nuspecFile));

            using (Stream stream = File.OpenRead(nuspecFile)) {
                // Don't validate the manifest since this might be a partial manifest
                // The bulk of the metadata might be coming from the project.
                Manifest manifest = Manifest.ReadFrom(stream, this);
                builder.Populate(manifest.Metadata);

                if (manifest.Files != null) {
                    string basePath = Path.GetDirectoryName(nuspecFile);
                    builder.PopulateFiles(basePath, manifest.Files);
                }
            }
        }

        private string GetNuspec() {
            return GetNuspecPaths().FirstOrDefault(File.Exists);
        }

        private IEnumerable<string> GetNuspecPaths() {
            // Check for a nuspec in the project file
            yield return GetContentOrNone(file => Path.GetExtension(file).Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase));
            // Check for a nuspec named after the project
            yield return Path.Combine(_project.DirectoryPath, Path.GetFileNameWithoutExtension(_project.FullPath) + Constants.ManifestExtension);
        }

        private string GetPackagesConfig() {
            return GetContentOrNone(file => Path.GetFileName(file).Equals(PackageReferenceRepository.PackageReferenceFile, StringComparison.OrdinalIgnoreCase));
        }

        private string GetContentOrNone(Func<string, bool> matcher) {
            return GetFiles("Content").Concat(GetFiles("None")).FirstOrDefault(matcher);
        }

        private IEnumerable<string> GetFiles(string itemType) {
            return _project.GetItems(itemType).Select(item => item.GetMetadataValue("FullPath"));
        }

        private void AddFiles(PackageBuilder builder, string itemType, string targetFolder) {
            // Get the content files from the project
            foreach (var item in _project.GetItems(itemType)) {
                string fullPath = item.GetMetadataValue("FullPath");

                if (_excludeFiles.Contains(Path.GetFileName(fullPath))) {
                    continue;
                }

                string targetFilePath = GetTargetPath(item);

                if (!File.Exists(fullPath)) {
                    Logger.Log(MessageLevel.Warning, NuGetResources.Warning_FileDoesNotExist, targetFilePath);
                    continue;
                }

                builder.Files.Add(new PhysicalPackageFile {
                    SourcePath = fullPath,
                    TargetPath = Path.Combine(targetFolder, targetFilePath)
                });
            }
        }

        private string GetTargetPath(ProjectItem item) {
            string path = item.UnevaluatedInclude;
            if (item.HasMetadata("Link")) {
                path = item.GetMetadataValue("Link");
            }
            return Normalize(path);
        }

        private string Normalize(string path) {
            string projectDirectoryPath = PathUtility.EnsureTrailingSlash(_project.DirectoryPath);
            string fullPath = PathUtility.GetAbsolutePath(projectDirectoryPath, path);

            // If the file is under the project root then remove the project root
            if (fullPath.StartsWith(projectDirectoryPath, StringComparison.OrdinalIgnoreCase)) {
                return fullPath.Substring(_project.DirectoryPath.Length).TrimStart(Path.DirectorySeparatorChar);
            }

            // Otherwise the file is probably a shortcut so just take the file name
            return Path.GetFileName(fullPath);
        }

        private class Walker : PackageWalker {
            private readonly IPackageRepository _repository;
            private readonly List<IPackage> _packages;

            public Walker(List<IPackage> packages) {
                _packages = packages;
                _repository = new PackageRepository(packages.ToList());
            }

            protected override IPackage ResolveDependency(PackageDependency dependency) {
                return _repository.FindDependency(dependency);
            }

            protected override bool OnAfterResolveDependency(IPackage package, IPackage dependency) {
                _packages.Remove(dependency);
                return base.OnAfterResolveDependency(package, dependency);
            }

            public IEnumerable<IPackage> GetMinimalSet() {
                foreach (var package in _repository.GetPackages()) {
                    Walk(package);
                }
                return _packages;
            }

            private class PackageRepository : PackageRepositoryBase {
                private readonly IEnumerable<IPackage> _packages;
                public PackageRepository(IEnumerable<IPackage> packages) {
                    _packages = packages;
                }

                public override string Source {
                    get { return null; }
                }

                public override IQueryable<IPackage> GetPackages() {
                    return _packages.AsQueryable();
                }
            }
        }

        private class ReverseTransformFormFile : IPackageFile {
            private readonly Lazy<Func<Stream>> _streamFactory;

            public ReverseTransformFormFile(IPackageFile file, IEnumerable<IPackageFile> transforms) {
                Path = file.Path + ".transform";
                _streamFactory = new Lazy<Func<Stream>>(() => ReverseTransform(file, transforms), isThreadSafe: false);
            }

            public string Path {
                get;
                private set;
            }

            public Stream GetStream() {
                return _streamFactory.Value();
            }

            private static Func<Stream> ReverseTransform(IPackageFile file, IEnumerable<IPackageFile> transforms) {
                // Get the original
                XElement element = GetElement(file);

                // Remove all the transforms
                foreach (var transformFile in transforms) {
                    element.Except(GetElement(transformFile));
                }

                // Create the stream with the transformed content
                var ms = new MemoryStream();
                element.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToStreamFactory();
            }

            private static XElement GetElement(IPackageFile file) {
                using (Stream stream = file.GetStream()) {
                    return XElement.Load(stream);
                }
            }
        }
    }
}
