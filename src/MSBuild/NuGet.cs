using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NuGet.MSBuild {
    public class NuGet : Task {
        private static readonly string SymbolsExtension = ".symbols" + Constants.PackageExtension;
        private readonly IFileSystemProvider _fileSystemProvider;

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
        private static readonly string[] _symbolPackageExcludes = new[]{
            @"content\**\*",
            @"tools\**\*.ps1"
        };


        public NuGet()
            : this(new FileSystemProvider()) {
        }

        public NuGet(IFileSystemProvider fileSystemProvider) {
            _fileSystemProvider = fileSystemProvider;
        }

        /// <summary>
        /// Base path to be used when building the package.
        /// </summary>
        public string BaseDir { get; set; }

        /// <summary>
        /// Path to the nuspec file used to build the package.
        /// </summary>
        [Required]
        public string SpecFile { get; set; }

        /// <summary>
        /// Directory to output nupack files to.
        /// </summary>
        public string PackageDir { get; set; }

        /// <summary>
        /// Path 
        /// </summary>
        [Output]
        public string OutputPackage { get; set; }

        public SemVer Version { get; set; }

        public bool Symbols { get; set; }

        public override bool Execute() {
            if (String.IsNullOrEmpty(BaseDir)) {
                BaseDir = Directory.GetCurrentDirectory();
            }

            var fileSystem = _fileSystemProvider.CreateFileSystem(BaseDir);

            if (String.IsNullOrEmpty(SpecFile)) {
                Log.LogError(Resources.NuGetResources.SpecFileMustNotBeEmpty);
                return false;
            }

            if (!fileSystem.FileExists(SpecFile)) {
                Log.LogError(Resources.NuGetResources.SpecFileDoesNotExist);
                return false;
            }

            PackageDir = Path.Combine(Directory.GetCurrentDirectory(), PackageDir ?? String.Empty);
            if (!fileSystem.DirectoryExists(PackageDir)) {
                Log.LogError(Resources.NuGetResources.PackageDirDoesNotExist);
                return false;
            }

            try {
                string packageFilePath = BuildPackage(fileSystem, SpecFile);

                OutputPackage = packageFilePath;

                Log.LogMessage(String.Format(
                    Resources.NuGetResources.CreatedPackage,
                    fileSystem.GetFullPath(SpecFile),
                    fileSystem.GetFullPath(packageFilePath)));
            }
            catch (Exception ex) {
                Log.LogError(Resources.NuGetResources.UnexpectedError, ex.ToString());
                return false;
            }

            return true;
        }

        private string BuildPackage(IFileSystem fileSystem, string specFilePath) {
            PackageBuilder packageBuilder;
            using (Stream stream = fileSystem.OpenFile(specFilePath)) {
                packageBuilder = new PackageBuilder(fileSystem.OpenFile(specFilePath), BaseDir);
            }

            if (Symbols) {
                // remove source related files when building the lib package
                ExcludeFilesForLibPackage(packageBuilder.Files);
            }

            string packageFilePath = GetOutputPath(packageBuilder);

            Log.LogMessage(Resources.NuGetResources.CreatingPackage,
                fileSystem.GetFullPath(specFilePath),
                fileSystem.GetFullPath(packageFilePath));

            BuildPackage(fileSystem, packageBuilder, packageFilePath);

            if (Symbols) {
                BuildSymbolsPackage(fileSystem, specFilePath);
            }

            return packageFilePath;
        }

        private void BuildPackage(IFileSystem fileSystem, PackageBuilder builder, string path, string outputPath = null) {
            if (Version != null) {
                builder.Version = Version;
            }
            outputPath = outputPath ?? GetOutputPath(builder);

            // If the BasePath is not specified, use the directory of the input file (nuspec / proj) file
            ExcludeFiles(builder.Files);

            // Track if the package file was already present on disk
            bool isExistingPackage = fileSystem.FileExists(outputPath);
            try {
                using (MemoryStream stream = new MemoryStream()) {
                    builder.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    fileSystem.AddFile(outputPath, stream);
                }
            }
            catch {
                if (!isExistingPackage && fileSystem.FileExists(outputPath)) {
                    File.Delete(outputPath);
                }
                throw;
            }
        }

        private void BuildSymbolsPackage(IFileSystem fileSystem, string specFilePath) {
            PackageBuilder symbolsBuilder;
            using (Stream stream = fileSystem.OpenFile(specFilePath)) {
                symbolsBuilder = new PackageBuilder(stream, BaseDir);
            }

            // remove unnecessary files when building the symbols package
            ExcludeFilesForSymbolPackage(symbolsBuilder.Files);

            string outputPath = GetOutputPath(symbolsBuilder, symbols: true);
            BuildPackage(fileSystem, symbolsBuilder, specFilePath, outputPath);
        }

        internal void ExcludeFiles(ICollection<IPackageFile> packageFiles) {
            // Always exclude the nuspec file
            // Review: This exclusion should be done by the package builder because it knows which file would collide with the auto-generated
            // manifest file.
            var wildCards = _defaultExcludes.Concat(new[] { @"**\*" + Constants.ManifestExtension });
            // The user has not explicitly disabled default filtering.
            wildCards = wildCards.Concat(_defaultExcludes);
            PathResolver.FilterPackageFiles(packageFiles, ResolvePath, wildCards);
        }

        internal static void ExcludeFilesForLibPackage(ICollection<IPackageFile> files) {
            PathResolver.FilterPackageFiles(files, file => file.Path, _libPackageExcludes);
        }

        internal static void ExcludeFilesForSymbolPackage(ICollection<IPackageFile> files) {
            PathResolver.FilterPackageFiles(files, file => file.Path, _symbolPackageExcludes);
        }

        internal string GetOutputPath(IPackageBuilder builder, bool symbols = false) {
            string version = builder.Version.ToString();

            // Output file is {id}.{version}
            string outputFile = builder.Id + "." + version;

            // If this is a source package then add .symbols.nupkg to the package file name
            if (symbols) {
                outputFile += SymbolsExtension;
            }
            else {
                outputFile += Constants.PackageExtension;
            }

            string outputDirectory = (PackageDir ?? Directory.GetCurrentDirectory());
            return Path.Combine(PackageDir, outputFile);
        }

        private string ResolvePath(IPackageFile packageFile) {
            var physicalPackageFile = packageFile as PhysicalPackageFile;
            // For PhysicalPackageFiles, we want to filter by SourcePaths, the path on disk. The Path value maps to the TargetPath
            if (physicalPackageFile == null) {
                return packageFile.Path;
            }
            var path = physicalPackageFile.SourcePath;
            int index = path.IndexOf(BaseDir, StringComparison.OrdinalIgnoreCase);
            if (index != -1) {
                // Since wildcards are going to be relative to the base path, remove the BaseDir portion of the file's source path. 
                // Also remove any leading path separator slashes
                path = path.Substring(index + BaseDir.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            return path;
        }
    }
}
