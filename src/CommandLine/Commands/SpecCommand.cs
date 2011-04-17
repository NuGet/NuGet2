using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "spec", "SpecCommandDescription", MaxArgs = 1, UsageSummaryResourceName = "SpecCommandUsageSummary")]
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
            var manifest = new Manifest();
            string projectFile = null;
            string fileName = null;

            if (!String.IsNullOrEmpty(AssemblyPath)) {
                // Extract metadata from the assembly
                string path = Path.Combine(Directory.GetCurrentDirectory(), AssemblyPath);
                AssemblyMetadata metadata = AssemblyMetadataExtractor.GetMetadata(path);
                manifest.Metadata.Id = metadata.Name;
                manifest.Metadata.Version = metadata.Version.ToString();
                manifest.Metadata.Authors = metadata.Company;
                manifest.Metadata.Description = metadata.Description;
            }
            else {
                if (!CommandLineUtility.TryGetProjectFile(out projectFile)) {
                    manifest.Metadata.Id = Arguments.Any() ? Arguments[0] : "Package";
                    manifest.Metadata.Version = "1.0";
                }
                else {
                    fileName = Path.GetFileNameWithoutExtension(projectFile);
                    manifest.Metadata.Id = "$id$";
                    manifest.Metadata.Version = "$version$";
                    manifest.Metadata.Description = "$description$";
                    manifest.Metadata.Authors = "$author$";
                }
            }

            // Get the file name from the id or the project file
            fileName = fileName ?? manifest.Metadata.Id;

            // If we're using a project file then we want the a minimal nuspec
            if (String.IsNullOrEmpty(projectFile)) {
                manifest.Metadata.Description = manifest.Metadata.Description ?? "Package description";
                if (String.IsNullOrEmpty(manifest.Metadata.Authors)) {
                    manifest.Metadata.Authors = Environment.UserName;
                }
                manifest.Metadata.Dependencies = new List<ManifestDependency>();
                manifest.Metadata.Dependencies.Add(new ManifestDependency { Id = "SampleDependency", Version = "1.0" });
            }

            manifest.Metadata.ProjectUrl = "http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE";
            manifest.Metadata.LicenseUrl = "http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE";
            manifest.Metadata.IconUrl = "http://ICON_URL_HERE_OR_DELETE_THIS_LINE";
            manifest.Metadata.Tags = "Tag1 Tag2";

            string nuspecFile = fileName + Constants.ManifestExtension;

            // Skip the creation if the file exists and force wasn't specified
            if (File.Exists(nuspecFile) && !Force) {
                Console.WriteLine(NuGetResources.SpecCommandFileExists, nuspecFile);
            }
            else {
                try {
                    using (Stream stream = File.Create(nuspecFile)) {
                        manifest.Save(stream, validate: false);
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
    }
}
