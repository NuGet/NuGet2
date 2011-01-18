using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;

using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Cmdlets {

    /// <summary>
    /// This command creates new package file.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Package")]
    public class NewPackageCmdlet : NuGetBaseCmdlet {
        private static readonly HashSet<string> _exclude =
            new HashSet<string>(new[] { Constants.PackageExtension, Constants.ManifestExtension }, StringComparer.OrdinalIgnoreCase);

        public NewPackageCmdlet()
            : this(ServiceLocator.GetInstance<ISolutionManager>(), 
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>()) {
        }

        public NewPackageCmdlet(ISolutionManager solutionManager, IVsPackageManagerFactory packageManagerFactory)
            : base(solutionManager, packageManagerFactory) {
        }

        [Parameter(Position = 0, ValueFromPipelineByPropertyName=true)]
        [ValidateNotNullOrEmpty]
        [Alias("Name")] // <EnvDTE.Project>.Name
        public string Project { get; set; }

        [Parameter(Mandatory=true, Position = 1)]
        [Alias("Spec")]
        [ValidateNotNullOrEmpty]
        public string SpecFile { get; set; }

        [Parameter(Mandatory=true, Position = 2)]
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

            string projectName = Project;

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
            using(Stream stream = File.Create(outputFilePath)) {
                builder.Save(stream);
            }
            WriteLine(Resources.Cmdlet_PackageCreated);
        }

        private string GetSpecFilePath(Project projectIns) {
            string filePath = null;
            string specFilePath = null;
            ProjectItem specFile = null;

            // spec file provided?
            if (SpecFile != null) {
                string errorMessage;
                bool? exists;

                // yes, so translate from PSPath to win32 path
                if (!TryTranslatePSPath(SpecFile, out filePath, out exists, out errorMessage)) {
                    // terminating
                    ErrorHandler.HandleException(
                        new ItemNotFoundException(Resources.Cmdlet_InvalidPathSyntax),
                        terminating: true,
                        errorId: NuGetErrorId.FileNotFound,
                        category: ErrorCategory.InvalidArgument,
                        target: SpecFile);
                }
            }
            try
            {
                specFile = FindSpecFile(projectIns, filePath).SingleOrDefault();
            }
            catch (InvalidOperationException)
            {
                // SingleOrDefault will throw if more than one spec files were found
                // terminating
                ErrorHandler.HandleException(
                    new InvalidOperationException(Resources.Cmdlet_TooManySpecFiles),
                    terminating: true,
                    errorId: NuGetErrorId.TooManySpecFiles,
                    category: ErrorCategory.InvalidOperation);
            }

            if (specFile == null)
            {
                // terminating
                ErrorHandler.HandleException(
                    new ItemNotFoundException(Resources.Cmdlet_NuspecFileNotFound),
                    terminating: true,
                    errorId: NuGetErrorId.NuspecFileNotFound,
                    category: ErrorCategory.ObjectNotFound,
                    target: SpecFile);
            }
            else
            {
                specFilePath = specFile.FileNames[0];
            }
            return specFilePath;
        }

        private string GetTargetFilePath(Project projectIns, PackageBuilder builder) {
            string outputFilePath = null;
            string filePath;
            bool? exists;
            string errorMessage;

            if (TryTranslatePSPath(TargetFile, out filePath, out exists, out errorMessage)) {

                // Get the output file path
                outputFilePath = GetPackageFilePath(filePath, projectIns.FullName, builder.Id, builder.Version);

                // prevent overwrite if -NoClobber specified
                if (exists == true && NoClobber.IsPresent) {
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
            }
            else {
                // terminating
                ErrorHandler.HandleException(
                    new ItemNotFoundException(Resources.Cmdlet_InvalidPathSyntax),
                    terminating: true,
                    errorId: NuGetErrorId.FileNotFound,
                    category: ErrorCategory.InvalidArgument,
                    target: SpecFile);
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
