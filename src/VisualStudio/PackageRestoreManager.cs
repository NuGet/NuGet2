using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio.Resources;

using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace NuGet.VisualStudio {
    [Export(typeof(IPackageRestoreManager))]
    internal class PackageRestoreManager : IPackageRestoreManager {
        private const string PackageRestoreFolder = ".nuget";
        private const string SolutionFolder = "nuget";
        private const string NuGetExeFile = ".nuget\\nuget.exe";
        private const string NuGetTargetsFile = ".nuget\\nuget.targets";
        private const string NuGetBuildPackageName = "NuGet.Build";

        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly ISolutionManager _solutionManager;
        private readonly IVsPackageManagerFactory _packageManagerFactory;
        private readonly IPackageRepositoryFactory _packageRepositoryFactory;
        private readonly IVsThreadedWaitDialogFactory _waitDialogFactory;
        private readonly DTE _dte;

        [ImportingConstructor]
        public PackageRestoreManager(
            ISolutionManager solutionManager, 
            IFileSystemProvider fileSystemProvider,
            IVsPackageManagerFactory packageManagerFactory,
            IPackageRepositoryFactory packageRepositoryFactory,
            ISettings settings) :
            this(ServiceLocator.GetInstance<DTE>(),
                 solutionManager,
                 fileSystemProvider,
                 packageManagerFactory,
                 packageRepositoryFactory,
                 ServiceLocator.GetGlobalService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>()) {
        }

        internal PackageRestoreManager(
            DTE dte,
            ISolutionManager solutionManager,
            IFileSystemProvider fileSystemProvider,
            IVsPackageManagerFactory packageManagerFactory,
            IPackageRepositoryFactory packageRepositoryFactory,
            IVsThreadedWaitDialogFactory waitDialogFactory) {

            _dte = dte;
            _fileSystemProvider = fileSystemProvider;
            _solutionManager = solutionManager;
            _packageManagerFactory = packageManagerFactory;
            _packageRepositoryFactory = packageRepositoryFactory;
            _waitDialogFactory = waitDialogFactory;
            _solutionManager.ProjectAdded += OnProjectAdded;
        }

        public bool IsCurrentSolutionEnabled {
            get {
                if (!_solutionManager.IsSolutionOpen) {
                    return false;
                }

                string solutionDirectory = _solutionManager.SolutionDirectory;
                if (String.IsNullOrEmpty(solutionDirectory)) {
                    return false;
                }

                IFileSystem fileSystem = _fileSystemProvider.GetFileSystem(solutionDirectory);
                return fileSystem.DirectoryExists(PackageRestoreFolder) &&
                       fileSystem.FileExists(NuGetExeFile) &&
                       fileSystem.FileExists(NuGetTargetsFile);
            }
        }

        public void EnableCurrentSolution(bool quietMode) {
            if (!_solutionManager.IsSolutionOpen) {
                throw new InvalidOperationException(VsResources.SolutionNotAvailable);
            }

            Exception exception = null;

            IVsThreadedWaitDialog2 waitDialog;
            _waitDialogFactory.CreateInstance(out waitDialog);
            try {
                waitDialog.StartWaitDialog(
                    VsResources.DialogTitle,
                    VsResources.PackageRestoreWaitMessage,
                    VsResources.PackageRestoreProgressMessage,
                    null,
                    null,
                    iDelayToShowDialog: 0,
                    fIsCancelable: false,
                    fShowMarqueeProgress: true);

                EnablePackageRestore();
            }
            catch (Exception ex) {
                exception = ex;
                ExceptionHelper.WriteToActivityLog(exception);
            }
            finally {
                int canceled;
                waitDialog.EndWaitDialog(out canceled);
            }

            if (!quietMode) {
                if (exception != null) {
                    // show error message
                    MessageHelper.ShowErrorMessage(
                        VsResources.PackageRestoreErrorMessage +
                            Environment.NewLine +
                            Environment.NewLine +
                            ExceptionUtility.Unwrap(exception).Message,
                        VsResources.DialogTitle);
                }
                else {
                    // show success message
                    MessageHelper.ShowInfoMessage(
                        VsResources.PackageRestoreCompleted,
                        VsResources.DialogTitle);
                }
            }
        }

        private void EnablePackageRestore() {
            EnsureNuGetBuild();

            foreach (Project project in _solutionManager.GetProjects()) {
                EnablePackageRestore(project);
            }
        }

        private void EnablePackageRestore(Project project) {
            if (project.IsWebSite()) {
                // Can't do anything with Website
                return;
            }

            MsBuildProject buildProject = project.AsMSBuildProject();
            AddNuGetTargets(project, buildProject);
            SetMsBuildProjectProperty(project, buildProject, "RestorePackages", "true");
        }

        private void AddNuGetTargets(Project project, MsBuildProject buildProject) {
            string targetsPath = Path.Combine(@"$(SolutionDir)", NuGetTargetsFile);

            AddSolutionDirProperty(project, buildProject);

            // adds an <Import> element to this project file.
            if (buildProject.Xml.Imports.All(
                    import => !import.Project.Equals(targetsPath, StringComparison.OrdinalIgnoreCase))) {

                buildProject.Xml.AddImport(targetsPath);
                project.Save();
                buildProject.ReevaluateIfNecessary();
            }
        }

        private void AddSolutionDirProperty(Project project, MsBuildProject buildProject) {
            const string SolutionDirProperty = "SolutionDir";

            if (buildProject.Xml.Properties == null ||
                buildProject.Xml.Properties.All(p => p.Name != SolutionDirProperty)) {

                string relativeSolutionPath = PathUtility.GetRelativePath(project.FullName, _solutionManager.SolutionDirectory);
                relativeSolutionPath = PathUtility.EnsureTrailingSlash(relativeSolutionPath);

                var solutionDirProperty = buildProject.Xml.AddProperty(SolutionDirProperty, relativeSolutionPath);
                solutionDirProperty.Condition = @"$(SolutionDir) == '' Or $(SolutionDir) == '*Undefined*'";

                project.Save();
            }
        }

        private static void SetMsBuildProjectProperty(Project project, MsBuildProject buildProject, string name, string value) {
            buildProject.SetProperty(name, value);
            project.Save();
        }

        private void EnsureNuGetBuild() {
            string solutionDirectory = _solutionManager.SolutionDirectory;
            string nugetToolsPath = Path.Combine(solutionDirectory, PackageRestoreFolder);
            
            if (!Directory.Exists(nugetToolsPath) || 
                !Directory.EnumerateFileSystemEntries(nugetToolsPath).Any()) {

                IVsPackageManager packageManager = GetPackageManagerForNuGetFeed();

                var installToolsPaths = new List<string>();
                EventHandler<PackageOperationEventArgs> installHandler = (sender, args) => {
                    installToolsPaths.Add(Path.Combine(args.TargetPath, Constants.ToolsDirectory));
                };

                try {
                    packageManager.PackageInstalled += installHandler;
                    // install the NuGet.Build package which contains MsBuild targets and settings for the projects
                    // this will also brings down NuGet.exe as part of the dependency of NuGet.Build
                    packageManager.InstallPackage(NuGetBuildPackageName, version: null, ignoreDependencies: false);

                    if (!Directory.Exists(nugetToolsPath)) {
                        Directory.CreateDirectory(nugetToolsPath);
                    }

                    // copy all files of NuGet.Build and NuGet.CommandLine packages to .nuget folder
                    installToolsPaths.ForEach(folder => FileHelper.CopyAllFiles(folder, nugetToolsPath));

                    // now add the .nuget folder to the solution as a solution folder.
                    _dte.Solution.AddFolderToSolution(SolutionFolder, nugetToolsPath);

                    DisableSourceControlMode();
                }
                finally {
                    packageManager.PackageInstalled -= installHandler;

                    if (packageManager.LocalRepository.Exists(NuGetBuildPackageName)) {
                        packageManager.UninstallPackage(
                            NuGetBuildPackageName,
                            version: null,
                            forceRemove: false,
                            removeDependencies: true);
                    }
                }
            }
        }

        private void DisableSourceControlMode() {
            // get the settings for this solution
            var settings = new Settings(_fileSystemProvider.GetFileSystem(_solutionManager.SolutionDirectory));
            settings.DisableSourceControlMode();
        }

        private void OnProjectAdded(object sender, ProjectEventArgs e) {
            if (IsCurrentSolutionEnabled) {
                EnablePackageRestore(e.Project);
            }
        }

        private IVsPackageManager GetPackageManagerForNuGetFeed() {
            IPackageRepository nugetRepository = _packageRepositoryFactory.CreateRepository(NuGetConstants.DefaultFeedUrl);
            return _packageManagerFactory.CreatePackageManager(
                nugetRepository,
                useFallbackForDependencies: false,
                addToRecent: false);
        }
    }
}