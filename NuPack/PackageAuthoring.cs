using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NuPack {
    public class PackageAuthoring {
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
                Console.ReadKey();
                // Parse the arguments. The last argument is the content to be added to the package
                var manifestFile = args.First();
                XmlManifestReader manifestReader = new XmlManifestReader(manifestFile);
                PackageBuilder builder = new PackageBuilder();
                manifestReader.ReadContentTo(builder);
                var destinationFile = String.Join(".", builder.Id, builder.Version, "nupack");
                using (Stream stream = File.Create(destinationFile)) {
                    builder.Save(stream);
                }
            }
            catch (Exception exception) {
                Console.Error.WriteLine(exception.Message);
            }
        }
    }
}
