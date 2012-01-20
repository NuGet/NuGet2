using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands
{
    [Command(typeof(NuGetResources), "pack", "PackageCommandDescription", MaxArgs = 1, UsageSummaryResourceName = "PackageCommandUsageSummary",
            UsageDescriptionResourceName = "PackageCommandUsageDescription", UsageExampleResourceName = "PackCommandUsageExamples")]
    public class PackCommand : Command
    {
        internal static readonly string SymbolsExtension = ".symbols" + Constants.PackageExtension;

        private static readonly string[] _defaultExcludes = new[] {
            // Exclude previous package files
            @"**\*" + Constants.PackageExtension, 
            // Exclude all files and directories that begin with "."
            @"**\\.**", ".**"
        };

        // Target file paths to exclude when building the lib package for symbol server scenario
        private static readonly string[] _libPackageExcludes = new[] {
            @"**\*.pdb",
            @"src\**\*"
        };

        // Target file paths to exclude when building the symbols package for symbol server scenario
        private static readonly string[] _symbolPackageExcludes = new[] {
            @"content\**\*",
            @"tools\**\*.ps1"
        };

        private readonly HashSet<string> _excludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> _allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {  
            Constants.ManifestExtension,
            ".csproj",
            ".vbproj",
            ".fsproj",
            ".nproj"
        };

        [Option(typeof(NuGetResources), "PackageCommandOutputDirDescription")]
        public string OutputDirectory { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandBasePathDescription")]
        public string BasePath { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandVerboseDescription")]
        public bool Verbose { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandVersionDescription")]
        public string Version { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandExcludeDescription")]
        public ICollection<string> Exclude
        {
            get { return _excludes; }
        }

        [Option(typeof(NuGetResources), "PackageCommandSymbolsDescription")]
        public bool Symbols { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandToolDescription")]
        public bool Tool { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandBuildDescription")]
        public bool Build { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandNoDefaultExcludes")]
        public bool NoDefaultExcludes { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandNoRunAnalysis")]
        public bool NoPackageAnalysis { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandPropertiesDescription")]
        public Dictionary<string, string> Properties
        {
            get
            {
                return _properties;
            }
        }

        [ImportMany]
        public IEnumerable<IPackageRule> Rules { get; set; }

        public override void ExecuteCommand()
        {
            // Get the input file
            string path = GetInputFile();

            Console.WriteLine(NuGetResources.PackageCommandAttemptingToBuildPackage, Path.GetFileName(path));

            IPackage package = BuildPackage(path);
            if (package != null && !NoPackageAnalysis)
            {
                AnalyzePackage(package);
            }
        }

        private IPackage BuildPackage(string path, PackageBuilder builder, string outputPath = null)
        {
            if (!String.IsNullOrEmpty(Version))
            {
                builder.Version = new SemanticVersion(Version);
            }

            outputPath = outputPath ?? GetOutputPath(builder);

            // If the BasePath is not specified, use the directory of the input file (nuspec / proj) file
            BasePath = String.IsNullOrEmpty(BasePath) ? Path.GetDirectoryName(Path.GetFullPath(path)) : BasePath;
            ExcludeFiles(builder.Files);
            // Track if the package file was already present on disk
            bool isExistingPackage = File.Exists(outputPath);
            try
            {
                using (Stream stream = File.Create(outputPath))
                {
                    builder.Save(stream);
                }
            }
            catch
            {
                if (!isExistingPackage && File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
                throw;
            }

            if (Verbose)
            {
                PrintVerbose(outputPath);
            }

            Console.WriteLine(NuGetResources.PackageCommandSuccess, outputPath);

            return new ZipPackage(outputPath);
        }

        private void PrintVerbose(string outputPath)
        {
            Console.WriteLine();
            var package = new ZipPackage(outputPath);

            Console.WriteLine("Id: {0}", package.Id);
            Console.WriteLine("Version: {0}", package.Version);
            Console.WriteLine("Authors: {0}", String.Join(", ", package.Authors));
            Console.WriteLine("Description: {0}", package.Description);
            if (package.LicenseUrl != null)
            {
                Console.WriteLine("License Url: {0}", package.LicenseUrl);
            }
            if (package.ProjectUrl != null)
            {
                Console.WriteLine("Project Url: {0}", package.ProjectUrl);
            }
            if (!String.IsNullOrEmpty(package.Tags))
            {
                Console.WriteLine("Tags: {0}", package.Tags.Trim());
            }
            if (package.Dependencies.Any())
            {
                Console.WriteLine("Dependencies: {0}", String.Join(", ", package.Dependencies.Select(d => d.ToString())));
            }
            else
            {
                Console.WriteLine("Dependencies: None");
            }

            Console.WriteLine();

            foreach (var file in package.GetFiles().OrderBy(p => p.Path))
            {
                Console.WriteLine(NuGetResources.PackageCommandAddedFile, file.Path);
            }

            Console.WriteLine();
        }

        internal void ExcludeFiles(ICollection<IPackageFile> packageFiles)
        {
            // Always exclude the nuspec file
            // Review: This exclusion should be done by the package builder because it knows which file would collide with the auto-generated
            // manifest file.
            var wildCards = _excludes.Concat(new[] { @"**\*" + Constants.ManifestExtension });
            if (!NoDefaultExcludes)
            {
                // The user has not explicitly disabled default filtering.
                wildCards = wildCards.Concat(_defaultExcludes);
            }
            PathResolver.FilterPackageFiles(packageFiles, ResolvePath, wildCards);
        }

        private string ResolvePath(IPackageFile packageFile)
        {
            var physicalPackageFile = packageFile as PhysicalPackageFile;
            // For PhysicalPackageFiles, we want to filter by SourcePaths, the path on disk. The Path value maps to the TargetPath
            if (physicalPackageFile == null)
            {
                return packageFile.Path;
            }
            var path = physicalPackageFile.SourcePath;
            int index = path.IndexOf(BasePath, StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                // Since wildcards are going to be relative to the base path, remove the BasePath portion of the file's source path. 
                // Also remove any leading path separator slashes
                path = path.Substring(index + BasePath.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            return path;
        }

        private string GetOutputPath(PackageBuilder builder, bool symbols = false)
        {
            string version = String.IsNullOrEmpty(Version) ? builder.Version.ToString() : Version;

            // Output file is {id}.{version}
            string outputFile = builder.Id + "." + version;

            // If this is a source package then add .symbols.nupkg to the package file name
            if (symbols)
            {
                outputFile += SymbolsExtension;
            }
            else
            {
                outputFile += Constants.PackageExtension;
            }

            string outputDirectory = OutputDirectory ?? Directory.GetCurrentDirectory();
            return Path.Combine(outputDirectory, outputFile);
        }

        private IPackage BuildPackage(string path)
        {
            string extension = Path.GetExtension(path);

            if (extension.Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase))
            {
                return BuildFromNuspec(path);
            }
            else
            {
                return BuildFromProjectFile(path);
            }
        }

        private IPackage BuildFromNuspec(string path)
        {
            PackageBuilder packageBuilder = CreatePackageBuilderFromNuspec(path);

            if (Symbols)
            {
                // remove source related files when building the lib package
                ExcludeFilesForLibPackage(packageBuilder.Files);

                if (!packageBuilder.Files.Any())
                {
                    throw new CommandLineException(String.Format(CultureInfo.CurrentCulture, NuGetResources.PackageCommandNoFilesForLibPackage,
                        path, CommandLineConstants.NuGetDocs));
                }
            }

            IPackage package = BuildPackage(path, packageBuilder);

            if (Symbols)
            {
                BuildSymbolsPackage(path);
            }

            return package;
        }

        private void BuildSymbolsPackage(string path)
        {
            PackageBuilder symbolsBuilder = CreatePackageBuilderFromNuspec(path);
            // remove unnecessary files when building the symbols package
            ExcludeFilesForSymbolPackage(symbolsBuilder.Files);

            if (!symbolsBuilder.Files.Any())
            {
                throw new CommandLineException(String.Format(CultureInfo.CurrentCulture, NuGetResources.PackageCommandNoFilesForSymbolsPackage,
                        path, CommandLineConstants.NuGetDocs));
            }

            string outputPath = GetOutputPath(symbolsBuilder, symbols: true);
            BuildPackage(path, symbolsBuilder, outputPath);
        }

        internal static void ExcludeFilesForLibPackage(ICollection<IPackageFile> files)
        {
            PathResolver.FilterPackageFiles(files, file => file.Path, _libPackageExcludes);
        }

        internal static void ExcludeFilesForSymbolPackage(ICollection<IPackageFile> files)
        {
            PathResolver.FilterPackageFiles(files, file => file.Path, _symbolPackageExcludes);
        }

        private PackageBuilder CreatePackageBuilderFromNuspec(string path)
        {
            // Set the version property if the flag is set
            if (!String.IsNullOrEmpty(Version))
            {
                Properties["version"] = Version;
            }

            // Initialize the property provider based on what was passed in using the properties flag
            var propertyProvider = new DictionaryPropertyProvider(Properties);

            if (String.IsNullOrEmpty(BasePath))
            {
                return new PackageBuilder(path, propertyProvider);
            }
            return new PackageBuilder(path, BasePath, propertyProvider);
        }

        private IPackage BuildFromProjectFile(string path)
        {
            var factory = new ProjectFactory(path)
            {
                IsTool = Tool,
                Logger = Console,
                Build = Build,
            };

            // Add the additional Properties to the properties of the Project Factory
            foreach (var property in Properties)
            {
                factory.Properties.Add(property.Key, property.Value);
            }

            // Create a builder for the main package as well as the sources/symbols package
            PackageBuilder mainPackageBuilder = factory.CreateBuilder();

            // Build the main package
            IPackage package = BuildPackage(path, mainPackageBuilder);

            // If we're excluding symbols then do nothing else
            if (!Symbols)
            {
                return package;
            }

            Console.WriteLine();
            Console.WriteLine(NuGetResources.PackageCommandAttemptingToBuildSymbolsPackage, Path.GetFileName(path));

            factory.IncludeSymbols = true;
            PackageBuilder symbolsBuilder = factory.CreateBuilder();
            symbolsBuilder.Version = mainPackageBuilder.Version;

            // Get the file name for the sources package and build it
            string outputPath = GetOutputPath(symbolsBuilder, symbols: true);
            BuildPackage(path, symbolsBuilder, outputPath);

            // this is the real package, not the symbol package
            return package;
        }

        internal void AnalyzePackage(IPackage package)
        {
            IEnumerable<IPackageRule> packageRules = Rules;
            if (!String.IsNullOrEmpty(package.Version.SpecialVersion))
            {
                // If a package contains a special token, we'll warn users if it does not strictly follow semver guidelines.
                packageRules = packageRules.Concat(new[] { new StrictSemanticVersionValidationRule() });
            }

            IList<PackageIssue> issues = package.Validate(packageRules).OrderBy(p => p.Title, StringComparer.CurrentCulture).ToList();

            if (issues.Count > 0)
            {
                Console.WriteLine();
                Console.WriteWarning(NuGetResources.PackageCommandPackageIssueSummary, issues.Count, package.Id);
                foreach (var issue in issues)
                {
                    PrintPackageIssue(issue);
                }
            }
        }

        private void PrintPackageIssue(PackageIssue issue)
        {
            Console.WriteLine();
            Console.WriteWarning(
                prependWarningText: false,
                value: NuGetResources.PackageCommandIssueTitle,
                args: issue.Title);

            Console.WriteWarning(
                prependWarningText: false,
                value: NuGetResources.PackageCommandIssueDescription,
                args: issue.Description);

            if (!String.IsNullOrEmpty(issue.Solution))
            {
                Console.WriteWarning(
                    prependWarningText: false,
                    value: NuGetResources.PackageCommandIssueSolution,
                    args: issue.Solution);
            }
        }

        private string GetInputFile()
        {
            IEnumerable<string> files;

            if (Arguments.Any())
            {
                files = Arguments;
            }
            else
            {
                files = Directory.GetFiles(Directory.GetCurrentDirectory());
            }

            return GetInputFile(files);
        }

        internal static string GetInputFile(IEnumerable<string> files)
        {
            var candidates = files.Where(file => _allowedExtensions.Contains(Path.GetExtension(file)))
                                  .ToList();

            switch (candidates.Count)
            {
                case 1:
                    return candidates.Single();
                case 2:
                    // Remove all nuspec files
                    candidates.RemoveAll(file => Path.GetExtension(file).Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase));
                    if (candidates.Count == 1)
                    {
                        return candidates.Single();
                    }
                    goto default;
                default:
                    throw new CommandLineException(NuGetResources.PackageCommandSpecifyInputFileError);
            }
        }

        private class DictionaryPropertyProvider : IPropertyProvider
        {
            private readonly IDictionary<string, string> _properties;

            public DictionaryPropertyProvider(IDictionary<string, string> properties)
            {
                _properties = properties;
            }

            public dynamic GetPropertyValue(string propertyName)
            {
                string value;
                if (_properties.TryGetValue(propertyName, out value))
                {
                    return value;
                }
                return null;
            }
        }
    }
}