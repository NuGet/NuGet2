using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "pack", "PackageCommandDescription", MaxArgs = 1,
        UsageSummaryResourceName = "PackageCommandUsageSummary", UsageDescriptionResourceName = "PackageCommandUsageDescription")]
    public class PackCommand : Command {
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

        [Option(typeof(NuGetResources), "PackageCommandDebugDescription")]
        public bool Debug { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandExcludeDescription")]
        public ICollection<string> Exclude {
            get { return _excludes; }
        }

        [Option(typeof(NuGetResources), "PackageCommandSourcesDescription")]
        public bool Sources { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandToolDescription")]
        public bool Tool { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandNoDefaultExcludes")]
        public bool NoDefaultExcludes { get; set; }

        public override void ExecuteCommand() {
            // Get the input file
            string path = GetInputFile();

            Console.WriteLine(NuGetResources.PackageCommandAttemptingToBuildPackage, Path.GetFileName(path));

            PackageBuilder builder = GetPackageBuilder(path);

            if (!String.IsNullOrEmpty(Version)) {
                builder.Version = new Version(Version);
            }

            // If the BasePath is not specified, use the directory of the input file (nuspec / proj) file
            BasePath = String.IsNullOrEmpty(BasePath) ? Path.GetDirectoryName(Path.GetFullPath(path)) : BasePath;
            ExcludeFiles(builder.Files);

            // Get the output path
            string outputPath = GetOutputPath(builder);

            try {
                using (Stream stream = File.Create(outputPath)) {
                    builder.Save(stream);
                }
            }
            catch {
                if (File.Exists(outputPath)) {
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

        private string GetOutputPath(PackageBuilder builder) {
            // Output file is {id}.{version}
            string outputFile = builder.Id + "." + builder.Version;


            // If this is a source package then add .Sources to the package file name
            if (Sources) {
                outputFile += ".sources";
            }

            // Add the extension
            outputFile += Constants.PackageExtension;

            string outputDirectory = OutputDirectory ?? Directory.GetCurrentDirectory();
            return Path.Combine(outputDirectory, outputFile);
        }

        private PackageBuilder GetPackageBuilder(string path) {
            string extension = Path.GetExtension(path).ToLowerInvariant();

            switch (extension) {
                case ".nuspec":
                    return BuildFromNuspec(path);
                default:
                    return BuildFromProjectFile(path);
            }
        }

        private PackageBuilder BuildFromNuspec(string path) {
            if (String.IsNullOrEmpty(BasePath)) {
                return new PackageBuilder(path);
            }

            return new PackageBuilder(path, BasePath);
        }

        private PackageBuilder BuildFromProjectFile(string path) {
            var projectBuilder = new ProjectPackageBuilder(path, Console) {
                IncludeSources = Sources,
                Debug = Debug,
                IsTool = Tool
            };

            return projectBuilder.BuildPackage();
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
