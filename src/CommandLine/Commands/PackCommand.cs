using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "pack", "PackageCommandDescription", MaxArgs = 1,
        UsageSummaryResourceName = "PackageCommandUsageSummary", UsageDescriptionResourceName = "PackageCommandUsageDescription")]
    public class PackCommand : Command {
        internal static readonly string SymbolsExtension = ".symbols" + Constants.PackageExtension;

        private static readonly string[] _defaultExcludes = new[] {
            // Exclude previous package files
            @"**\*" + Constants.PackageExtension, 
            // Exclude all files and directories that begin with "."
            @"**\\.**", ".**"
        };

        private readonly HashSet<string> _excludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> _allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {  
            Constants.ManifestExtension,
            ".csproj",
            ".vbproj",
            ".fsproj",
        };

        [Option(typeof(NuGetResources), "PackageCommandOutputDirDescription")]
        public string OutputDirectory { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandBasePathDescription")]
        public string BasePath { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandVerboseDescription")]
        public bool Verbose { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandVersionDescription")]
        public string Version { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandConfigurationDescription")]
        public string Configuration { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandExcludeDescription")]
        public ICollection<string> Exclude {
            get { return _excludes; }
        }

        [Option(typeof(NuGetResources), "PackageCommandSymbolsDescription")]
        public bool Symbols { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandToolDescription")]
        public bool Tool { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandNoDefaultExcludes")]
        public bool NoDefaultExcludes { get; set; }

        public override void ExecuteCommand() {
            // Get the input file
            string path = GetInputFile();

            Console.WriteLine(NuGetResources.PackageCommandAttemptingToBuildPackage, Path.GetFileName(path));

            BuildPackage(path);
        }

        private void BuildPackage(string path, PackageBuilder builder, string outputPath = null) {
            if (!String.IsNullOrEmpty(Version)) {
                builder.Version = new Version(Version);
            }

            outputPath = outputPath ?? GetOutputPath(builder);

            // If the BasePath is not specified, use the directory of the input file (nuspec / proj) file
            BasePath = String.IsNullOrEmpty(BasePath) ? Path.GetDirectoryName(Path.GetFullPath(path)) : BasePath;
            ExcludeFiles(builder.Files);
            // Track if the package file was already present on disk
            bool isExistingPackage = File.Exists(outputPath);
            try {
                using (Stream stream = File.Create(outputPath)) {
                    builder.Save(stream);
                }
            }
            catch {
                if (!isExistingPackage && File.Exists(outputPath)) {
                    File.Delete(outputPath);
                }
                throw;
            }

            if (Verbose) {
                PrintVerbose(outputPath);
            }


            Console.WriteLine(NuGetResources.PackageCommandSuccess, outputPath);
        }

        private void PrintVerbose(string outputPath) {
            Console.WriteLine();
            var package = new ZipPackage(outputPath);

            Console.WriteLine("Id: {0}", package.Id);
            Console.WriteLine("Version: {0}", package.Version);
            Console.WriteLine("Authors: {0}", String.Join(", ", package.Authors));
            Console.WriteLine("Description: {0}", package.Description);
            if (package.LicenseUrl != null) {
                Console.WriteLine("License Url: {0}", package.LicenseUrl);
            }
            if (package.ProjectUrl != null) {
                Console.WriteLine("Project Url: {0}", package.ProjectUrl);
            }
            if (!String.IsNullOrEmpty(package.Tags)) {
                Console.WriteLine("Tags: {0}", package.Tags.Trim());
            }
            if (package.Dependencies.Any()) {
                Console.WriteLine("Dependencies: {0}", String.Join(", ", package.Dependencies.Select(d => d.ToString())));
            }
            else {
                Console.WriteLine("Dependencies: None");
            }

            Console.WriteLine();

            foreach (var file in package.GetFiles().OrderBy(p => p.Path)) {
                Console.WriteLine(NuGetResources.PackageCommandAddedFile, file.Path);
            }

            Console.WriteLine();
        }

        internal void ExcludeFiles(ICollection<IPackageFile> packageFiles) {
            // Always exclude the nuspec file
            // Review: This exclusion should be done by the package builder because it knows which file would collide with the auto-generated
            // manifest file.
            var wildCards = _excludes.Concat(new[] { @"**\*" + Constants.ManifestExtension });
            if (!NoDefaultExcludes) {
                // The user has not explicitly disabled default filtering.
                wildCards = wildCards.Concat(_defaultExcludes);
            }
            PathResolver.FilterPackageFiles(packageFiles, ResolvePath, wildCards);
        }

        private string ResolvePath(IPackageFile packageFile) {
            var physicalPackageFile = packageFile as PhysicalPackageFile;
            // For PhysicalPackageFiles, we want to filter by SourcePaths, the path on disk. The Path value maps to the TargetPath
            if (physicalPackageFile == null) {
                return packageFile.Path;
            }
            var path = physicalPackageFile.SourcePath;
            int index = path.IndexOf(BasePath, StringComparison.OrdinalIgnoreCase);
            if (index != -1) {
                // Since wildcards are going to be relative to the base path, remove the BasePath portion of the file's source path. 
                // Also remove any leading path separator slashes
                path = path.Substring(index + BasePath.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            return path;
        }

        private string GetOutputPath(PackageBuilder builder, bool symbols = false) {
            string version = String.IsNullOrEmpty(Version) ? builder.Version.ToString() : Version;

            // Output file is {id}.{version}
            string outputFile = builder.Id + "." + version;

            // If this is a source package then add .symbols.nupkg to the package file name
            if (symbols) {
                outputFile += SymbolsExtension;
            }
            else {
                outputFile += Constants.PackageExtension;
            }

            string outputDirectory = OutputDirectory ?? Directory.GetCurrentDirectory();
            return Path.Combine(outputDirectory, outputFile);
        }

        private void BuildPackage(string path) {
            string extension = Path.GetExtension(path).ToLowerInvariant();

            switch (extension) {
                case ".nuspec":
                    BuildFromNuspec(path);
                    break;
                default:
                    BuildFromProjectFile(path);
                    break;
            }
        }

        private void BuildFromNuspec(string path) {
            PackageBuilder builder = null;

            if (String.IsNullOrEmpty(BasePath)) {
                builder = new PackageBuilder(path);
            }
            else {
                builder = new PackageBuilder(path, BasePath);
            }

            BuildPackage(path, builder);
        }

        private void BuildFromProjectFile(string path) {
            var factory = new ProjectFactory(path) {
                IsTool = Tool,
                Logger = Console
            };

            // Specify the configuration
            factory.Properties.Add("Configuration", Configuration ?? "Release");

            // Create a builder for the main package as well as the sources/symbols package
            PackageBuilder mainPackageBuilder = factory.CreateBuilder();
            // Build the main package
            BuildPackage(path, mainPackageBuilder);


            // If we're excluding symbols then do nothing else
            if (!Symbols) {
                return;
            }

            Console.WriteLine();
            Console.WriteLine(NuGetResources.PackageCommandAttemptingToBuildSymbolsPackage, Path.GetFileName(path));

            factory.IncludeSymbols = true;
            PackageBuilder symbolsBuilder = factory.CreateBuilder();
            // Get the file name for the sources package and build it
            string outputPath = GetOutputPath(symbolsBuilder, symbols: true);
            BuildPackage(path, symbolsBuilder, outputPath);
        }

        private string GetInputFile() {
            IEnumerable<string> files = null;

            if (Arguments.Any()) {
                files = Arguments;
            }
            else {
                files = Directory.GetFiles(Directory.GetCurrentDirectory());
            }

            var candidates = files.Where(file => _allowedExtensions.Contains(Path.GetExtension(file)))
                                  .ToList();

            switch (candidates.Count) {
                case 1:
                    return candidates.Single();
                default:
                    throw new CommandLineException(NuGetResources.PackageCommandSpecifyInputFileError);
            }
        }
    }
}