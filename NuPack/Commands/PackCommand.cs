namespace NuGet {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using Common;


    [Export(typeof(ICommand))]
    [Command(typeof(NuGetResources), "pack", "PackageCommandDescription", MinArgs = 0, MaxArgs = 1,
        UsageSummaryResourceName = "PackageCommandUsageSummary", UsageDescriptionResourceName = "PackageCommandUsageDescription")]
    public class PackCommand : ICommand {
        private static readonly HashSet<string> _exclude =
            new HashSet<string>(new[] { Constants.PackageExtension, Constants.ManifestExtension }, StringComparer.OrdinalIgnoreCase);

        public List<string> Arguments { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandOutputDirDescription", AltName = "o")]
        public string OutputDirectory { get; set; }

        [Option(typeof(NuGetResources), "PackageCommandBasePathDescription", AltName = "b")]
        public string BasePath { get; set; }

        public void Execute() {
            string nuspecFile;

            if (Arguments.Any()) {
                nuspecFile = Arguments[0];
            }
            else {
                string[] possibleNuspecFiles = GetNuSpecFilesInDirectory();
                if (possibleNuspecFiles.Length == 1) {
                    nuspecFile = possibleNuspecFiles[0];
                }
                else {
                    throw new CommandLineException(NuGetResources.PackageCommandSpecifyNuSpecFileError);
                }
            }

            PackageBuilder builder = new PackageBuilder(nuspecFile, BasePath ?? Path.GetDirectoryName(nuspecFile));

            var outputFile = String.Join(".", builder.Id, builder.Version, Constants.PackageExtension.TrimStart('.'));

            // Remove the output file or the package spec might try to include it (which is default behavior)
            builder.Files.RemoveAll(file => _exclude.Contains(Path.GetExtension(file.Path)));

            string outputPath = Path.Combine(OutputDirectory ?? Directory.GetCurrentDirectory(), outputFile);

            using (Stream stream = File.Create(outputPath)) {
                builder.Save(stream);
            }

            Console.WriteLine(NuGetResources.PackageCommandSuccess, outputPath);
        }

        private static string[] GetNuSpecFilesInDirectory() {
            return Directory.GetFiles(Directory.GetCurrentDirectory(), "*" + Constants.ManifestExtension);
        }
    }
}
