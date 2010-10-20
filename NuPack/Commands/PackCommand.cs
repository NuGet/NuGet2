namespace NuPack {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using Common;


    [Export(typeof(ICommand))]
    [Command(typeof(NuPackResources), "pack", "PackageCommandDescription", MinArgs = 0, MaxArgs = 1,
        UsageSummaryResourceName = "PackageCommandUsageSummary", UsageDescriptionResourceName = "PackageCommandUsageDescription")]
    public class PackCommand : ICommand {
        private static readonly HashSet<string> _exclude =
            new HashSet<string>(new[] { Constants.PackageExtension, Constants.ManifestExtension }, StringComparer.OrdinalIgnoreCase);

        public List<string> Arguments { get; set; }

        [Option(typeof(NuPackResources), "PackageCommandOutputDirDescription", AltName = "outdir")]
        public string OutputDirectory { get; set; }

        [Option(typeof(NuPackResources), "PackageCommandBasePathDescription", AltName = "base")]
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
                    throw new CommandLineException("Please specify a nuspec file to use.");
                }
            }

            PackageBuilder builder = PackageBuilder.ReadFrom(nuspecFile, BasePath ?? Path.GetDirectoryName(nuspecFile));
            builder.Created = DateTime.Now;
            builder.Modified = DateTime.Now;
            var outputFile = String.Join(".", builder.Id, builder.Version, Constants.PackageExtension.TrimStart('.'));

            // Remove the output file or the package spec might try to include it (which is default behavior)
            builder.Files.RemoveAll(file => _exclude.Contains(Path.GetExtension(file.Path)));

            string outputPath = Path.Combine(OutputDirectory ?? Directory.GetCurrentDirectory(), outputFile);

            using (Stream stream = File.Create(outputPath)) {
                builder.Save(stream);
            }

            Console.WriteLine(NuPackResources.PackageCommandSuccess, outputPath);
        }

        private static string[] GetNuSpecFilesInDirectory() {
            return Directory.GetFiles(Directory.GetCurrentDirectory(), "*" + Constants.ManifestExtension);
        }
    }
}
