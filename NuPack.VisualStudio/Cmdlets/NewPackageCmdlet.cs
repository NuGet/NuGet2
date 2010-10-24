using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using EnvDTE;
using NuPack.VisualStudio.Resources;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This command creates new package file.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Package")]
    public class NewPackageCmdlet : NuPackBaseCmdlet {
        [Parameter(Position = 0)]
        public string Project { get; set; }

        [Parameter(Position = 1)]
        [Alias("Spec")]
        public string SpecFile { get; set; }

        [Parameter(Position = 2)]
        public string TargetFile { get; set; }

        protected override void ProcessRecordCore() {

            string projectName = Project;
            if (String.IsNullOrEmpty(projectName)) {
                projectName = DefaultProjectName;
            }

            if (String.IsNullOrEmpty(projectName)) {
                WriteError(VsResources.Cmdlet_MissingProjectParameter);
                return;
            }

            var projectIns = GetProjectFromName(projectName);
            if (projectIns == null) {
                WriteError(String.Format(CultureInfo.CurrentCulture, VsResources.Cmdlet_ProjectNotFound, projectName));
                return;
            }

            var specItem = FindSpecFile(projectIns, SpecFile);
            if (specItem == null) {
                WriteError(VsResources.Cmdlet_NuspecFileNotFound);
                return;
            }

            var specFilePath = specItem.FileNames[0];
            PackageBuilder builder;
            using (Stream stream = File.OpenRead(specFilePath)) {
                builder = new PackageBuilder(stream);
            }
            string outputFile = TargetFile;
            File.Delete(outputFile);

            if (String.IsNullOrEmpty(outputFile)) {
                outputFile = String.Join(".", builder.Manifest.Metadata.Id, builder.Manifest.Metadata.Version, Constants.PackageExtension.TrimStart('.'));
            }

            if (!Path.IsPathRooted(outputFile)) {
                // if the path is a relative, prepend the project path to it
                string folder = Path.GetDirectoryName(projectIns.FullName);
                outputFile = Path.Combine(folder, outputFile);
            }

            WriteLine(String.Format(CultureInfo.CurrentCulture, VsResources.Cmdlet_CreatingPackage, outputFile));

            using (Stream stream = File.Create(outputFile)) {
                builder.Save(stream);
            }

            WriteLine(VsResources.Cmdlet_PackageCreated);
        }

        private static ProjectItem FindSpecFile(EnvDTE.Project projectIns, string specFile) {
            if (!String.IsNullOrEmpty(specFile)) {
                return projectIns.ProjectItems.Item(specFile);
            }
            else {
                // Verify if the project has exactly one file with the .nuspec extension. 
                // If found, use it as the manifest file for package creation.
                int count = 0;
                ProjectItem foundItem = null;

                foreach (ProjectItem item in projectIns.ProjectItems) {
                    if (item.Name.EndsWith(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase)) {
                        foundItem = item;
                        count++;
                        if (count > 1) {
                            break;
                        }
                    }
                }

                return (count == 1) ? foundItem : null;
            }
        }
    }
}