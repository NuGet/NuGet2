using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio.Resources;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace NuGet.VisualStudio
{
    [Export(typeof(IPackageRestoreManager))]
    internal class PackageRestoreManager : IPackageRestoreManager
    {
        private const string DotNuGetFolder = ".nuget";
        private const string NuGetExeFile = ".nuget\\nuget.exe";
        private const string NuGetTargetsFile = ".nuget\\nuget.targets";
        private const string NuGetBuildPackageName = "NuGet.Build";
        private const string NuGetCommandLinePackageName = "NuGet.CommandLine";

        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly ISolutionManager _solutionManager;
        private readonly IPackageRepositoryFactory _packageRepositoryFactory;
        private readonly IVsThreadedWaitDialogFactory _waitDialogFactory;
        private readonly IPackageRepository _localCacheRepository;
        private readonly DTE _dte;

        [ImportingConstructor]
        public PackageRestoreManager(
            ISolutionManager solutionManager,
            IFileSystemProvider fileSystemProvider,
            IPackageRepositoryFactory packageRepositoryFactory) :
            this(ServiceLocator.GetInstance<DTE>(),
                 solutionManager,
                 fileSystemProvider,
                 packageRepositoryFactory,
                 MachineCache.Default,
                 ServiceLocator.GetGlobalService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>())
        {
        }

        internal PackageRestoreManager(
            DTE dte,
            ISolutionManager solutionManager,
            IFileSystemProvider fileSystemProvider,
            IPackageRepositoryFactory packageRepositoryFactory,
            IPackageRepository localCacheRepository,
            IVsThreadedWaitDialogFactory waitDialogFactory)
        {

            Debug.Assert(solutionManager != null);
            _dte = dte;
            _fileSystemProvider = fileSystemProvider;
            _solutionManager = solutionManager;
            _packageRepositoryFactory = packageRepositoryFactory;
            _waitDialogFactory = waitDialogFactory;
            _localCacheRepository = localCacheRepository;
            _solutionManager.ProjectAdded += OnProjectAdded;
        }

        public bool IsCurrentSolutionEnabled
        {
            get
            {
                if (!_solutionManager.IsSolutionOpen)
                {
                    return false;
                }

                string solutionDirectory = _solutionManager.SolutionDirectory;
                if (String.IsNullOrEmpty(solutionDirectory))
                {
                    return false;
                }

                IFileSystem fileSystem = new PhysicalFileSystem(solutionDirectory);
                return fileSystem.DirectoryExists(DotNuGetFolder) &&
                       fileSystem.FileExists(NuGetExeFile) &&
                       fileSystem.FileExists(NuGetTargetsFile);
            }
        }

        public void EnableCurrentSolution(bool quietMode)
        {
            if (!_solutionManager.IsSolutionOpen)
            {
                throw new InvalidOperationException(VsResources.SolutionNotAvailable);
            }

            if (!quietMode)
            {
                // if not in quiet mode, ask user for confirmation before proceeding
                bool? result = MessageHelper.ShowQueryMessage(
                    VsResources.PackageRestoreConfirmation,
                    VsResources.DialogTitle,
                    showCancelButton: false);
                if (result != true)
                {
                    return;
                }
            }

            Exception exception = null;

            IVsThreadedWaitDialog2 waitDialog;
            _waitDialogFactory.CreateInstance(out waitDialog);
            try
            {
                waitDialog.StartWaitDialog(
                    VsResources.DialogTitle,
                    VsResources.PackageRestoreWaitMessage,
                    String.Empty,
                    null,
                    null,
                    iDelayToShowDialog: 0,
                    fIsCancelable: false,
                    fShowMarqueeProgress: true);

                EnablePackageRestore();
            }
            catch (Exception ex)
            {
                exception = ex;
                ExceptionHelper.WriteToActivityLog(exception);
            }
            finally
            {
                int canceled;
                waitDialog.EndWaitDialog(out canceled);
            }

            if (!quietMode)
            {
                if (exception != null)
                {
                    // show error message
                    MessageHelper.ShowErrorMessage(
                        VsResources.PackageRestoreErrorMessage +
                            Environment.NewLine +
                            Environment.NewLine +
                            ExceptionUtility.Unwrap(exception).Message,
                        VsResources.DialogTitle);
                }
                else
                {
                    // show success message
                    MessageHelper.ShowInfoMessage(
                        VsResources.PackageRestoreCompleted,
                        VsResources.DialogTitle);
                }
            }
        }

        private void EnablePackageRestore()
        {
            EnsureNuGetBuild();

            foreach (Project project in _solutionManager.GetProjects())
            {
                EnablePackageRestore(project);
            }
        }

        private void EnablePackageRestore(Project project)
        {
            if (project.IsWebSite())
            {
                // Can't do anything with Website
                return;
            }

            MsBuildProject buildProject = project.AsMSBuildProject();

            AddSolutionDirProperty(project, buildProject);
            AddNuGetTargets(project, buildProject);
            SetMsBuildProjectProperty(project, buildProject, "RestorePackages", "true");

            if (project.IsJavaScriptProject())
            {
                // JavaScript project requires an extra kick
                // in order to save changes to the project file.
                // TODO: Check with VS team to ask them to fix 
                buildProject.Save();
            }
        }

        private void AddNuGetTargets(Project project, MsBuildProject buildProject)
        {
            string targetsPath = Path.Combine(@"$(SolutionDir)", NuGetTargetsFile);

            // adds an <Import> element to this project file.
            if (buildProject.Xml.Imports == null ||
                buildProject.Xml.Imports.All(import => !targetsPath.Equals(import.Project, StringComparison.OrdinalIgnoreCase)))
            {
                buildProject.Xml.AddImport(targetsPath);
                project.Save();
                buildProject.ReevaluateIfNecessary();
            }
        }

        private void AddSolutionDirProperty(Project project, MsBuildProject buildProject)
        {
            const string solutiondir = "SolutionDir";

            if (buildProject.Xml.Properties == null ||
                buildProject.Xml.Properties.All(p => p.Name != solutiondir))
            {

                string relativeSolutionPath = PathUtility.GetRelativePath(project.FullName, PathUtility.EnsureTrailingSlash(_solutionManager.SolutionDirectory));
                relativeSolutionPath = PathUtility.EnsureTrailingSlash(relativeSolutionPath);

                var solutionDirProperty = buildProject.Xml.AddProperty(solutiondir, relativeSolutionPath);
                solutionDirProperty.Condition =
                    String.Format(
                        CultureInfo.InvariantCulture,
                        @"$({0}) == '' Or $({0}) == '*Undefined*'",
                        solutiondir);

                project.Save();
            }
        }

        private static void SetMsBuildProjectProperty(Project project, MsBuildProject buildProject, string name, string value)
        {
            buildProject.SetProperty(name, value);
            project.Save();
        }

        private void EnsureNuGetBuild()
        {
            string solutionDirectory = _solutionManager.SolutionDirectory;
            if (String.IsNullOrEmpty(solutionDirectory))
            {
                Debug.Fail("_solutionManager.SolutionDirectory == null");
                return;
            }
            string nugetFolderPath = Path.Combine(solutionDirectory, DotNuGetFolder);

            IFileSystem fileSystem = _fileSystemProvider.GetFileSystem(solutionDirectory);

            if (!fileSystem.DirectoryExists(DotNuGetFolder) ||
                !fileSystem.FileExists(NuGetExeFile) ||
                !fileSystem.FileExists(NuGetTargetsFile))
            {
                // download NuGet.Build and NuGet.CommandLine packages into the .nuget folder
                IPackageRepository nugetRepository = _packageRepositoryFactory.CreateRepository(NuGetConstants.DefaultFeedUrl);
                var installPackages = new string[] { NuGetBuildPackageName, NuGetCommandLinePackageName };
                foreach (var packageId in installPackages)
                {
                    IPackage package = GetPackage(nugetRepository, packageId); 
                    if (package == null)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                CultureInfo.InvariantCulture,
                                VsResources.PackageRestoreDownloadPackageFailed,
                                packageId));
                    }

                    fileSystem.AddFiles(package.GetFiles(Constants.ToolsDirectory), DotNuGetFolder, preserveFilePath: false);
                }

                // now add the .nuget folder to the solution as a solution folder.
                _dte.Solution.AddFolderToSolution(DotNuGetFolder, nugetFolderPath);

                DisableSourceControlMode();
            }
        }

        /// <summary>
        /// Try to retrieve the package with the specified Id from machine cache first. 
        /// If not found, download it from the specified repository and add to machine cache.
        /// </summary>
        private IPackage GetPackage(IPackageRepository repository, string packageId)
        {
            // first, find the package from the remote repository
            IPackage package = repository.GetLatestPackageById(packageId);
            if (package == null)
            {
                return null;
            }

            bool fromCache = false;

            IPackage cachedPackage = _localCacheRepository.FindPackage(packageId, package.Version);
            if (cachedPackage != null)
            {
                var dataServicePackage = package as DataServicePackage;
                if (dataServicePackage != null)
                {
                    if (dataServicePackage.PackageHash == cachedPackage.GetHash())
                    {
                        // if the remote package has the same hash as with the one in the machine cache, use the one from machine cache
                        package = cachedPackage;
                        fromCache = true;
                    }
                    else
                    {
                        // if the hash has changed, delete the stale package
                        _localCacheRepository.RemovePackage(cachedPackage);
                    }
                }
            }

            if (!fromCache)
            {
                _localCacheRepository.AddPackage(package);

                // swap to the Zip package to avoid potential downloading package twice
                package = _localCacheRepository.FindPackage(package.Id, package.Version);
                Debug.Assert(package != null);
            }

            return package;
        }

        private void DisableSourceControlMode()
        {
            // get the settings for this solution
            var nugetFolder = Path.Combine(_solutionManager.SolutionDirectory, DotNuGetFolder);
            var settings = new Settings(_fileSystemProvider.GetFileSystem(nugetFolder));
            settings.DisableSourceControlMode();
        }

        private void OnProjectAdded(object sender, ProjectEventArgs e)
        {
            if (IsCurrentSolutionEnabled)
            {
                EnablePackageRestore(e.Project);
            }
        }
    }
}