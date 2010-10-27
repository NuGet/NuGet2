using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio.Cmdlets {

    /// <summary>
    /// This command creates new package file.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Package")]
    public class NewPackageCmdlet : NuGetBaseCmdlet {
        private static readonly HashSet<string> _exclude =
            new HashSet<string>(new[] { Constants.PackageExtension, Constants.ManifestExtension }, StringComparer.OrdinalIgnoreCase);

        public NewPackageCmdlet()
            : this(NuGet.VisualStudio.SolutionManager.Current, DefaultVsPackageManagerFactory.Instance) {
        }

        public NewPackageCmdlet(ISolutionManager solutionManager, IVsPackageManagerFactory packageManagerFactory)
            : base(solutionManager, packageManagerFactory) {
        }

        [Parameter(Position = 0)]
        public string Project { get; set; }

        [Parameter(Position = 1)]
        [Alias("Spec")]
        public string SpecFile { get; set; }

        [Parameter(Position = 2)]
        public string TargetFile { get; set; }

        protected override void ProcessRecordCore() {
            if (!SolutionManager.IsSolutionOpen) {
                throw new InvalidOperationException(VsResources.Cmdlet_NoSolution);
            }

            string projectName = Project;
            if (String.IsNullOrEmpty(projectName)) {
                projectName = SolutionManager.DefaultProjectName;
            }

            if (String.IsNullOrEmpty(projectName)) {
                WriteError(VsResources.Cmdlet_MissingProjectParameter);
                return;
            }

            var projectIns = SolutionManager.GetProject(projectName);
            if (projectIns == null) {
                WriteError(String.Format(CultureInfo.CurrentCulture, VsResources.Cmdlet_ProjectNotFound, projectName));
                return;
            }

            ProjectItem specFile;
            try {
                specFile = FindSpecFile(projectIns, SpecFile).SingleOrDefault();
            }
            catch (InvalidOperationException) {
                // Single would throw if more than one spec files were found
                WriteError(VsResources.Cmdlet_TooManySpecFiles);
                return;
            }
            if (specFile == null) {
                WriteError(VsResources.Cmdlet_NuspecFileNotFound);
                return;
            }
            string specFilePath = specFile.FileNames[0];
            

            var builder = new NuGet.PackageBuilder(specFilePath);
            
            // Get the output file path
            string outputFile = GetPackageFilePath(TargetFile, projectIns.FullName, builder.Id, builder.Version);
            // Remove .nuspec and .nupkg files from output package 
            RemoveExludedFiles(builder);
            
            WriteLine(String.Format(CultureInfo.CurrentCulture, VsResources.Cmdlet_CreatingPackage, outputFile));
            using(Stream stream = File.Create(outputFile)) {
                builder.Save(stream);
            }
            WriteLine(VsResources.Cmdlet_PackageCreated);
        }

        internal static string GetPackageFilePath(string outputFile, string projectPath, string id, Version version) {
            if (String.IsNullOrEmpty(outputFile)) {
                outputFile = String.Join(".", id, version, Constants.PackageExtension.TrimStart('.'));
            }

            if (!Path.IsPathRooted(outputFile)) {
                // if the path is a relative, prepend the project path to it
                string folder = Path.GetDirectoryName(projectPath);
                outputFile = Path.Combine(folder, outputFile);
            }

            return outputFile;
        }

        internal static void RemoveExludedFiles(PackageBuilder builder) {
            // Remove the output file or the package spec might try to include it (which is default behavior)
            builder.Files.RemoveAll(file => _exclude.Contains(Path.GetExtension(file.Path)));
        }

        private static IEnumerable<ProjectItem> FindSpecFile(EnvDTE.Project projectIns, string specFile) {
            if (!String.IsNullOrEmpty(specFile)) {
                yield return projectIns.ProjectItems.Item(specFile);
            }
            else {
                // Verify if the project has exactly one file with the .nuspec extension. 
                // If found, use it as the manifest file for package creation.
                int count = 0;
                ProjectItem foundItem = null;

                foreach (ProjectItem item in projectIns.ProjectItems) {
                    if (item.Name.EndsWith(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase)) {
                        foundItem = item;
                        yield return foundItem;
                        count++;
                        if (count > 1) {
                            yield break;
                        }
                    }
                }
            }
        }
    }
}
