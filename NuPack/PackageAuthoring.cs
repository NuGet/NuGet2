using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NuPack.Common;
using OPC = System.IO.Packaging;

namespace NuPack {
    public class PackageAuthoring {
        public static void Main(string[] args) {
            // Review: Need to use a command-line parsing library instead of parsing it this way.
            string executable = Path.GetFileName(Environment.GetCommandLineArgs().First());
            string Usage = String.Format(CultureInfo.InvariantCulture,
                "Usage: {0} [/id:<id> /version:<version>] [/manifest:<file>] /out:<file> <content directory>", executable);
            if (!args.Any()) {
                Console.Error.WriteLine(Usage);
                return;
            }

            string packagePath = null;
            try {
                // Parse the arguments. The last argument is the content to be added to the package
                var parsedParams = ParseArguments(args.Take(args.Length - 1));

                // Identify the name of the package provided in the /out parameter
                packagePath = GetPackagePath(parsedParams);

                using (var package = OPC.Package.Open(packagePath, FileMode.Create)) {
                    // Read the manifest file if present and add properties, config and dependencies specified in it to the package
                    ReadManifestFile(package, parsedParams);

                    // If Id and version are specified via the command line, assign them to the package properties
                    SetPropertiesFromArguments(package, parsedParams);

                    // Add content to the package
                    ReadContent(package, args.Last());

                    // Make sure we have an id and a version
                    VerifyPackageBuild(package);
                }
            }
            catch (ArgumentException exception) {
                DeletePackage(packagePath);
                Console.Error.WriteLine(exception.Message);
                Console.Error.WriteLine(Usage);
            }
            catch (Exception exception) {
                DeletePackage(packagePath);
                Console.Error.WriteLine(exception.Message);
            }
        }

        internal static string GetPackagePath(IDictionary<string, string> parsedParams) {
            string outputFile = null;
            if (parsedParams.TryGetValue("out", out outputFile)) {
                return outputFile;
            }
            throw new ArgumentException(NuPackResources.OutputFileNotSpecified);
        }

        internal static void SetPropertiesFromArguments(OPC.Package package, IDictionary<string, string> parsedParams) {
            string value = null;
            if (parsedParams.TryGetValue("id", out value)) {
                package.PackageProperties.Identifier = value;
            }
            if (parsedParams.TryGetValue("version", out value)) {
                package.PackageProperties.Version = value;
            }
        }

        internal static void ReadManifestFile(OPC.Package package, IDictionary<string, string> parsedParams) {
            string value = null;
            if (parsedParams.TryGetValue("manifest", out value)) {
                PackageBuilder.ApplyManifest(package, value);
            }
        }

        internal static void ReadContent(OPC.Package package, string contentFolders) {
            foreach (var folder in contentFolders.Split(',')) {
                PackageBuilder.AddPackageContent(package, folder);
            }
        }

        internal static void VerifyPackageBuild(OPC.Package package) {
            if (String.IsNullOrEmpty(package.PackageProperties.Identifier) || String.IsNullOrEmpty(package.PackageProperties.Version)) {
                throw new ArgumentException(NuPackResources.IdVersionNotSpecified);
            }
        }

        internal static void DeletePackage(string packageFile) {
            try {
                File.Delete(packageFile);
            }
            catch(IOException) { }
        }

        internal static IDictionary<string, string> ParseArguments(IEnumerable<string> arguments) {
            Regex argumentRegex = new Regex(@"^(-|/)(?<param>[^:=$]+)((:|=)(?<value>[^$]+))?");
            Dictionary<string, string> parsedParams = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var arg in arguments) {
                Match match = argumentRegex.Match(arg);
                if (match.Success) {
                    string param = match.Groups["param"].Value;
                    string value = match.Groups["value"].Success ? match.Groups["value"].Value : null;
                    parsedParams[param] = value;
                }
            }
            return parsedParams;
        }
    }
}
