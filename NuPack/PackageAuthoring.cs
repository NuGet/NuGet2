using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NuPack {
    public class PackageAuthoring {
        private static readonly HashSet<string> _exclude = new HashSet<string>(new[] { ".nupack", ".nuspec" },
                                                                              StringComparer.OrdinalIgnoreCase);

        public static void Main(string[] args) {
            // Review: Need to use a command-line parsing library instead of parsing it this way.
            string executable = Path.GetFileName(Environment.GetCommandLineArgs().First());
            string Usage = String.Format(CultureInfo.InvariantCulture,
                "Usage: {0} <manifest-file>", executable);
            if (!args.Any()) {
                Console.Error.WriteLine(Usage);
                return;
            }

            try {
                // Parse the arguments. The last argument is the content to be added to the package
                var manifestFile = args.First();
                PackageBuilder builder = PackageBuilder.ReadFrom(manifestFile);
                builder.Created = DateTime.Now;
                builder.Modified = DateTime.Now;
                var outputFile = String.Join(".", builder.Id, builder.Version, "nupack");

                // Remove the output file or the package spec might try to include it (which is default behavior)
                builder.Files.RemoveAll(file => _exclude.Contains(Path.GetExtension(file.Path)));

                using (Stream stream = File.Create(outputFile)) {
                    builder.Save(stream);
                }

                Console.WriteLine("{0} created successfully", outputFile);
            }
            catch (Exception exception) {
                Console.Error.WriteLine(exception.Message);
            }
        }
    }
}
