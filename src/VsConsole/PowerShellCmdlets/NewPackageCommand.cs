using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands {

    /// <summary>
    /// This command creates new package file.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Package")]
    public class NewPackageCommand : NuGetBaseCommand {
        private static readonly HashSet<string> _exclude =
            new HashSet<string>(new[] { Constants.PackageExtension, Constants.ManifestExtension }, StringComparer.OrdinalIgnoreCase);

        public NewPackageCommand()
            : this(ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(), 
                   ServiceLocator.GetInstance<IHttpClientEvents>()) {
        }

        public NewPackageCommand(ISolutionManager solutionManager, IVsPackageManagerFactory packageManagerFactory, IHttpClientEvents httpClientEvents)
            : base(solutionManager, packageManagerFactory, httpClientEvents) {
        }

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string ProjectName { get; set; }

        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        public string SpecFileName { get; set; }

        [Parameter(Position = 2)]
        [ValidateNotNullOrEmpty]
        public string TargetFile { get; set; }

        /// <summary>
        /// If present, New-Package will not overwrite TargetFile.
        /// </summary>
        [Parameter]
        public SwitchParameter NoClobber { get; set; }

        protected override void ProcessRecordCore() {
            if (!SolutionManager.IsSolutionOpen) {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            string projectName = ProjectName;

            // no project specified - choose default
            if (String.IsNullOrEmpty(projectName)) {
                projectName = SolutionManager.DefaultProjectName;
            }

            // no default project? empty solution or no compatible projects found
            if (String.IsNullOrEmpty(projectName)) {
                ErrorHandler.ThrowNoCompatibleProjectsTerminatingError();
            }

            var projectIns = SolutionManager.GetProject(projectName);
            if (projectIns == null) {
                ErrorHandler.WriteProjectNotFoundError(projectName, terminating: true);
            }

            string specFilePath = GetSpecFilePath(projectIns);

            var builder = new NuGet.PackageBuilder(specFilePath);

            string outputFilePath = GetTargetFilePath(projectIns, builder);

            // Remove .nuspec and .nupkg files from output package 
            RemoveExludedFiles(builder);

            WriteLine(String.Format(CultureInfo.CurrentCulture, Resources.Cmdlet_CreatingPackage, outputFilePath));
            using (Stream stream = File.Create(outputFilePath)) {
                builder.Save(stream);
            }
            WriteLine(Resources.Cmdlet_PackageCreated);
        }

        private string GetSpecFilePath(Project projectIns) {
            string specFilePath = null;
            ProjectItem specFile = null;

            try {
                specFile = FindSpecFile(projectIns, SpecFileName).SingleOrDefault();
            }
            catch (InvalidOperationException) {
                // SingleOrDefault will throw if more than one spec files were found
                // terminating
                ErrorHandler.HandleException(
                    new InvalidOperationException(Resources.Cmdlet_TooManySpecFiles),
                    terminating: true,
                    errorId: NuGetErrorId.TooManySpecFiles,
                    category: ErrorCategory.InvalidOperation);
            }

            if (specFile == null) {
                // terminating
                ErrorHandler.HandleException(
                    new ItemNotFoundException(Resources.Cmdlet_NuspecFileNotFound),
                    terminating: true,
                    errorId: NuGetErrorId.NuspecFileNotFound,
                    category: ErrorCategory.ObjectNotFound,
                    target: SpecFileName);
            }
            else {
                specFilePath = specFile.FileNames[0];
            }

            return specFilePath;
        }

        private string GetTargetFilePath(Project projectIns, PackageBuilder builder) {
            // Get the output file path
            string outputFilePath = GetPackageFilePath(TargetFile, projectIns.FullName, builder.Id, builder.Version);
            
            bool fileExists = File.Exists(outputFilePath);
            // prevent overwrite if -NoClobber specified
            if (fileExists && NoClobber.IsPresent) {
                // terminating
                ErrorHandler.HandleException(
                    new UnauthorizedAccessException(String.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Cmdlet_FileExistsNoClobber, TargetFile)),
                    terminating: true,
                    errorId: NuGetErrorId.FileExistsNoClobber,
                    category: ErrorCategory.PermissionDenied,
                    target: TargetFile);
            }
                        
            return outputFilePath;
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
                ProjectItem projectItem = null;
                projectIns.ProjectItems.TryGetFile(specFile, out projectItem);
                yield return projectItem;
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