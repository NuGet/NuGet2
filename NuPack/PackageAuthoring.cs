namespace NuPack {

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    public class PackageAuthoring {

        private static readonly HashSet<string> _exclude = new HashSet<string>(new[] { Constants.PackageExtension, Constants.ManifestExtension }, StringComparer.OrdinalIgnoreCase);

        public static void Main(string[] args) {
            // Review: Need to use a command-line parsing library instead of parsing it this way.
            string executable = Path.GetFileName(Environment.GetCommandLineArgs().First());

            if (!args.Any()) {
                bool showUsage = true;
                string usage = String.Format(CultureInfo.InvariantCulture, "Usage: {0} <manifest-file>", executable);
                string[] nuspecFiles = GetNuSpecFilesInDirectory();

                if (nuspecFiles != null) {
                    switch (nuspecFiles.Length) {
                        case 0:
                            break;
                        case 1:
                            showUsage = false;
                            args = nuspecFiles;
                            break;
                        default:
                            usage = "Specify which nuspec file to use because this folder has more than one.";
                            break;
                    }
                }

                if (showUsage) {
                    Console.Error.WriteLine(usage);
                    Environment.Exit(1);
                }
            }

            try {
                var manifestFile = args.First();
                string outputDirectory = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();
                PackageBuilder builder = PackageBuilder.ReadFrom(manifestFile);
                builder.Created = DateTime.Now;
                builder.Modified = DateTime.Now;
                var outputFile = String.Join(".", builder.Id, builder.Version, Constants.PackageExtension.TrimStart('.'));

                // Remove the output file or the package spec might try to include it (which is default behavior)
                builder.Files.RemoveAll(file => _exclude.Contains(Path.GetExtension(file.Path)));

                string outputPath = Path.Combine(outputDirectory, outputFile);

                using (Stream stream = File.Create(outputPath)) {
                    builder.Save(stream);
                }

                Console.WriteLine("{0} created successfully", outputPath);
            }
            catch (Exception exception) {
                Console.Error.WriteLine(exception.Message);
            }
        }

        private static string[] GetNuSpecFilesInDirectory() {
            return Directory.GetFiles(Directory.GetCurrentDirectory(), "*" + Constants.ManifestExtension);
        }
    }
}