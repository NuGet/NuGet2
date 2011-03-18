using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "spec", "SpecCommandDescription", MaxArgs = 0)]
    public class SpecCommand : Command {
        [Option(typeof(NuGetResources), "SpecCommandAssemblyPathDescription")]
        public string AssemblyPath {
            get;
            set;
        }

        [Option(typeof(NuGetResources), "SpecCommandForceDescription")]
        public bool Force {
            get;
            set;
        }

        public override void ExecuteCommand() {
            var builder = new PackageBuilder();
            if (!String.IsNullOrEmpty(AssemblyPath)) {
                AssemblyMetadataExtractor.ExtractMetadata(AssemblyPath, builder);                
            }
            else {
                builder.Id = "Package";
                builder.Version = new Version("1.0");
            }

            builder.Description = builder.Description ?? "Package description";
            if (!builder.Authors.Any()) {
                builder.Authors.Add("Author here");
            }

            builder.Owners.Add("Owner here");
            builder.ProjectUrl = new Uri("http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE");
            builder.LicenseUrl = new Uri("http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE");
            builder.IconUrl = new Uri("http://ICON_URL_HERE_OR_DELETE_THIS_LINE");
            builder.Tags.Add("Tag1");
            builder.Tags.Add("Tag2");
            builder.Dependencies.Add(new PackageDependency("SampleDependency", VersionUtility.ParseVersionSpec("1.0")));

            string nuspecFile = builder.Id + ".nuspec";

            // Skip the creation if the file exists and force wasn't specified
            if (File.Exists(nuspecFile) && !Force) {
                Console.WriteLine(NuGetResources.SpecCommandFileExists, nuspecFile);
            }
            else {
                try {
                    using (Stream stream = File.Create(nuspecFile)) {
                        Manifest.Create(builder).Save(stream);
                    }

                    Console.WriteLine(NuGetResources.SpecCommandCreatedNuSpec, nuspecFile);
                }
                catch {
                    // Cleanup the file if it fails to save for some reason
                    File.Delete(nuspecFile);
                    throw;
                }
            }
        }

        private string GetAttributeValueOrDefault<T>(Assembly assembly, Func<T, string> selector) where T : Attribute {
            // Get the attribute
            T attribute = assembly.GetCustomAttributes(typeof(T), inherit: false).Cast<T>().FirstOrDefault();

            if (attribute != null) {
                string value = selector(attribute);
                // Return the value only if it isn't null or empty so that we can use ?? to fall back
                if (!String.IsNullOrEmpty(value)) {
                    return value;
                }
            }
            return null;
        }
    }
}
