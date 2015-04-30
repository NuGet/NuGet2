using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio.Resources;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

#if VS10 || VS11 || VS12
using NuGetVS = NuGet.VisualStudio12;
#endif

#if VS14
using NuGetVS = NuGet.VisualStudio14;
#endif

namespace NuGet.VisualStudio
{
    [Export(typeof(IPackageRestoreManager))]
    internal class PackageRestoreManager : IPackageRestoreManager
    {
        private static readonly string NuGetExeFile = Path.Combine(VsConstants.NuGetSolutionSettingsFolder, "NuGet.exe");
        private static readonly string NuGetTargetsFile = Path.Combine(VsConstants.NuGetSolutionSettingsFolder, "NuGet.targets");
        private const string NuGetBuildPackageName = "NuGet.Build";
        private const string NuGetCommandLinePackageName = "NuGet.CommandLine";

        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IVsPackageSourceProvider _packageSourceProvider;
        private readonly ISolutionManager _solutionManager;
        private readonly IPackageRepositoryFactory _packageRepositoryFactory;
        private readonly IVsThreadedWaitDialogFactory _waitDialogFactory;
        private readonly IPackageRepository _localCacheRepository;
        private readonly IVsPackageManagerFactory _packageManagerFactory;
        private readonly DTE _dte;
        private readonly ISettings _settings;
        private IPackageRepository _officialNuGetRepository;

        [ImportingConstructor]
        public PackageRestoreManager(
            ISolutionManager solutionManager,
            IFileSystemProvider fileSystemProvider,
            IPackageRepositoryFactory packageRepositoryFactory,
            IVsPackageManagerFactory packageManagerFactory,
            IVsPackageSourceProvider packageSourceProvider,
            IVsPackageInstallerEvents packageInstallerEvents,
            ISettings settings) :
            this(ServiceLocator.GetInstance<DTE>(),
                 solutionManager,
                 fileSystemProvider,
                 packageRepositoryFactory,
                 packageSourceProvider,
                 packageManagerFactory,
                 packageInstallerEvents,
                 MachineCache.Default,
                 ServiceLocator.GetGlobalService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>(),
                 settings)
        {
        }

        internal PackageRestoreManager(
            DTE dte,
            ISolutionManager solutionManager,
            IFileSystemProvider fileSystemProvider,
            IPackageRepositoryFactory packageRepositoryFactory,
            IVsPackageSourceProvider packageSourceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            IVsPackageInstallerEvents packageInstallerEvents,
            IPackageRepository localCacheRepository,
            IVsThreadedWaitDialogFactory waitDialogFactory,
            ISettings settings)
        {
            Debug.Assert(solutionManager != null);
            _dte = dte;
            _fileSystemProvider = fileSystemProvider;
            _solutionManager = solutionManager;
            _packageRepositoryFactory = packageRepositoryFactory;
            _packageSourceProvider = packageSourceProvider;
            _waitDialogFactory = waitDialogFactory;
            _packageManagerFactory = packageManagerFactory;
            _localCacheRepository = localCacheRepository;
            _settings = settings;
            _solutionManager.ProjectAdded += OnProjectAdded;
            _solutionManager.SolutionOpened += OnSolutionOpenedOrClosed;
            _solutionManager.SolutionClosed += OnSolutionOpenedOrClosed;
            packageInstallerEvents.PackageReferenceAdded += OnPackageReferenceAdded;
        }

        public bool IsCurrentSolutionEnabledForRestore
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

                IFileSystem fileSystem = _fileSystemProvider.GetFileSystem(solutionDirectory);
                return fileSystem.FileExists(NuGetExeFile) &&
                       fileSystem.FileExists(NuGetTargetsFile);
            }
        }

        public void EnableCurrentSolutionForRestore(bool fromActivation)
        {
            Exception exception = null;
            IVsThreadedWaitDialog2 waitDialog;
            _waitDialogFactory.CreateInstance(out waitDialog);
            try
            {
                if (!_solutionManager.IsSolutionOpen)
                {
                    throw new InvalidOperationException(VsResources.SolutionNotAvailable);
                }

                if (fromActivation)
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

                // Start the wait dialog on the UI thread
                InvokeOnUIThread(() =>
                    waitDialog.StartWaitDialog(
                        VsResources.DialogTitle,
                        VsResources.PackageRestoreWaitMessage,
                        String.Empty,
                        varStatusBmpAnim: null,
                        szStatusBarText: null,
                        iDelayToShowDialog: 0,
                        fIsCancelable: false,
                        fShowMarqueeProgress: true));

                if (fromActivation)
                {
                    // only enable package restore consent if this is called as a result of user enabling package restore
                    SetPackageRestoreConsent();
                }

                EnablePackageRestore(fromActivation);
            }
            catch (Exception ex)
            {
                exception = ex;
                ExceptionHelper.WriteToActivityLog(exception);
            }
            finally
            {
                InvokeOnUIThread(() =>
                {
                    int canceled;
                    waitDialog.EndWaitDialog(out canceled);
                });
            }

            if (fromActivation)
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

        public event EventHandler<PackagesMissingStatusEventArgs> PackagesMissingStatusChanged = delegate { };

        public Task RestoreMissingPackages()
        {
            TaskScheduler uiScheduler;
            try
            {
                uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            }
            catch (InvalidOperationException)
            {
                // this exception occurs during unit tests
                uiScheduler = TaskScheduler.Default;
            }

            Task task = Task.Factory.StartNew(() =>
            {
                IVsPackageManager packageManager = _packageManagerFactory.CreatePackageManagerWithAllPackageSources();
                IPackageRepository localRepository = packageManager.LocalRepository;
                var projectReferences = GetAllPackageReferences(packageManager);
                foreach (var reference in projectReferences)
                {
                    if (!localRepository.Exists(reference.Id, reference.Version))
                    {
                        packageManager.InstallPackage(reference.Id, reference.Version, ignoreDependencies: true, allowPrereleaseVersions: true);
                    }
                }
            });

            task.ContinueWith(originalTask =>
            {
                if (originalTask.IsFaulted)
                {
                    ExceptionHelper.WriteToActivityLog(originalTask.Exception);
                }
                else
                {
                    // we don't allow canceling
                    Debug.Assert(!originalTask.IsCanceled);

                    // after we're done with restoring packages, do the check again
                    CheckForMissingPackages();
                }
            }, uiScheduler);

            return task;
        }

        public void CheckForMissingPackages()
        {
            // This method is called by both Solution Opened and Solution Closed event handlers.
            // In the case of Solution Closed event, the _solutionManager.IsSolutionOpen is false,
            // and so we won't do the unnecessary work of checking for package references.
            bool missing = _solutionManager.IsSolutionOpen && CheckForMissingPackagesCore();
            PackagesMissingStatusChanged(this, new PackagesMissingStatusEventArgs(missing));
        }

        private void EnablePackageRestore(bool fromActivation)
        {
            EnsureNuGetBuild(fromActivation);

            IVsPackageManager packageManager = _packageManagerFactory.CreatePackageManagerWithAllPackageSources();
            foreach (Project project in _solutionManager.GetProjects())
            {
                EnablePackageRestore(project, packageManager);
            }
        }

        private void SetPackageRestoreConsent()
        {
            var consent = new PackageRestoreConsent(_settings);
            if (!consent.IsGranted)
            {
                consent.IsGrantedInSettings = true;
            }
        }

        private void EnablePackageRestore(Project project, IVsPackageManager packageManager)
        {
            var projectManager = packageManager.GetProjectManager(project);
            var projectPackageReferences = GetPackageReferences(projectManager);
            if (projectPackageReferences.IsEmpty())
            {
                // don't enable package restore for the project if it doesn't have at least one 
                // nuget package installed
                return;
            }

            EnablePackageRestore(project);
        }

        private void EnablePackageRestore(Project project)
        {
            if (project.IsWebSite())
            {
                // Can't do anything with Website
                return;
            }

            if (!VsVersionHelper.IsVisualStudio2010 &&
                (project.IsJavaScriptProject() ||  project.IsNativeProject()))
            {
                if (VsVersionHelper.IsVisualStudio2012)
                {
                    EnablePackageRestoreInVs2012(project);
                }
                else
                {
                    EnablePackageRestoreInVs2013(project);
                }
            }
            else
            {
                MsBuildProject buildProject = project.AsMSBuildProject();
                EnablePackageRestore(project, buildProject, saveProjectWhenDone: true);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnablePackageRestoreInVs2013(Project project)
        {
            NuGetVS.ProjectHelper.DoWorkInWriterLock(
                project,
                project.ToVsHierarchy(),
                buildProject => EnablePackageRestore(project, buildProject, saveProjectWhenDone: false));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnablePackageRestoreInVs2012(Project project)
        {
            project.DoWorkInWriterLock(
                buildProject => EnablePackageRestore(project, buildProject, saveProjectWhenDone: false));

            // When inside the Write lock, calling Project.Save() will cause a deadlock.
            // Thus we will save it after and outside of the Write lock.
            project.Save();
        }

        private void EnablePackageRestore(Project project, MsBuildProject buildProject, bool saveProjectWhenDone)
        {
            AddSolutionDirProperty(buildProject);
            AddNuGetTargets(buildProject);
            SetMsBuildProjectProperty(buildProject, "RestorePackages", "true");
            if (saveProjectWhenDone)
            {
                project.Save();
            }
        }

        private void AddNuGetTargets(MsBuildProject buildProject)
        {
            string targetsPath = Path.Combine(@"$(SolutionDir)", NuGetTargetsFile);
            NuGet.MSBuildProjectUtility.AddImportStatement(buildProject, targetsPath, ProjectImportLocation.Bottom);
        }

        private void AddSolutionDirProperty(MsBuildProject buildProject)
        {
            const string solutiondir = "SolutionDir";

            if (buildProject.Xml.Properties == null ||
                buildProject.Xml.Properties.All(p => p.Name != solutiondir))
            {
                string relativeSolutionPath = PathUtility.GetRelativePath(
                    buildProject.FullPath,
                    PathUtility.EnsureTrailingSlash(_solutionManager.SolutionDirectory));
                relativeSolutionPath = PathUtility.EnsureTrailingSlash(relativeSolutionPath);

                var solutionDirProperty = buildProject.Xml.AddProperty(solutiondir, relativeSolutionPath);
                solutionDirProperty.Condition =
                    String.Format(
                        CultureInfo.InvariantCulture,
                        @"$({0}) == '' Or $({0}) == '*Undefined*'",
                        solutiondir);
            }
        }

        private static void SetMsBuildProjectProperty(MsBuildProject buildProject, string name, string value)
        {
            if (!value.Equals(buildProject.GetPropertyValue(name), StringComparison.OrdinalIgnoreCase))
            {
                buildProject.SetProperty(name, value);
            }
        }

        private void EnsureNuGetBuild(bool fromActivation)
        {
            string solutionDirectory = _solutionManager.SolutionDirectory;
            string nugetFolderPath = Path.Combine(solutionDirectory, VsConstants.NuGetSolutionSettingsFolder);

            IFileSystem fileSystem = _fileSystemProvider.GetFileSystem(solutionDirectory);

            if (!fileSystem.DirectoryExists(VsConstants.NuGetSolutionSettingsFolder) ||
                !fileSystem.FileExists(NuGetExeFile) ||
                !fileSystem.FileExists(NuGetTargetsFile))
            {
                // download NuGet.Build and NuGet.CommandLine packages into the .nuget folder,
                // using the active package source first and fall back to other enabled package sources.
                IPackageRepository repository = CreatePackageRestoreRepository();

                var installPackages = new string[] { NuGetBuildPackageName, NuGetCommandLinePackageName };
                foreach (var packageId in installPackages)
                {
                    IPackage package = GetPackage(repository, packageId, fromActivation);
                    if (package == null)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                CultureInfo.CurrentCulture,
                                VsResources.PackageRestoreDownloadPackageFailed,
                                packageId));
                    }

                    fileSystem.AddFiles(package.GetFiles(Constants.ToolsDirectory), VsConstants.NuGetSolutionSettingsFolder, preserveFilePath: false);
                }

                // IMPORTANT: do this BEFORE adding the .nuget folder to solution so that 
                // the generated .nuget\nuget.config is included in the solution folder too. 
                DisableSourceControlMode();

                // now add the .nuget folder to the solution as a solution folder.
                _dte.Solution.AddFolderToSolution(VsConstants.NuGetSolutionSettingsFolder, nugetFolderPath);
            }
        }

        private IPackageRepository CreatePackageRestoreRepository()
        {
            var activeSource = _packageSourceProvider.ActivePackageSource;

            if (activeSource == null)
            {
                return _packageSourceProvider.CreateAggregateRepository(_packageRepositoryFactory, ignoreFailingRepositories: true);
            }

            IPackageRepository activeRepository = _packageRepositoryFactory.CreateRepository(activeSource.Source);
            if (AggregatePackageSource.IsAggregate(activeSource))
            {
                return activeRepository;
            }

            return _packageSourceProvider.CreatePriorityPackageRepository(_packageRepositoryFactory, activeRepository);
        }

        /// <summary>
        /// Try to retrieve the package with the specified Id from machine cache first. 
        /// If not found, download it from the specified repository and add to machine cache.
        /// </summary>
        private IPackage GetPackage(IPackageRepository repository, string packageId, bool fromActivation)
        {
            // first, find the package from the remote repository
            IPackage package = repository.FindPackage(packageId, version: null, allowPrereleaseVersions: true, allowUnlisted: false);

            if (package == null && fromActivation)
            {
                // if we can't find the package from the remote repositories, look for it
                // from nuget.org feed, provided that it's not already specified in one of the remote repositories
                if (!ContainsSource(_packageSourceProvider, NuGetConstants.DefaultFeedUrl) &&
                    !ContainsSource(_packageSourceProvider, NuGetConstants.V2LegacyFeedUrl))
                {
                    if (_officialNuGetRepository == null)
                    {
                        _officialNuGetRepository = _packageRepositoryFactory.CreateRepository(NuGetConstants.DefaultFeedUrl);
                    }

                    package = _officialNuGetRepository.FindPackage(packageId, version: null, allowPrereleaseVersions: true, allowUnlisted: false);
                }
            }

            bool fromCache = false;

            // if package == null, we use whatever version is in the machine cache
            IPackage cachedPackage = _localCacheRepository.FindPackage(packageId, package != null ? package.Version : null);
            if (cachedPackage != null)
            {
                var dataServicePackage = package as DataServicePackage;
                if (dataServicePackage != null)
                {
                    var cachedHash = cachedPackage.GetHash(dataServicePackage.PackageHashAlgorithm);
                    if (!dataServicePackage.PackageHash.Equals(cachedHash, StringComparison.OrdinalIgnoreCase))
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
                else if (package == null)
                {
                    // in this case, we didn't find the package from remote repository.
                    // fallback to using the one in the machine cache.
                    package = cachedPackage;
                    fromCache = true;
                }
            }

            if (package == null)
            {
                return null;
            }

            if (!fromCache)
            {
                if (!_localCacheRepository.Exists(package))
                {
                    _localCacheRepository.AddPackage(package);
                }

                // swap to the Zip package to avoid potential downloading package twice
                package = _localCacheRepository.FindPackage(package.Id, package.Version);
                Debug.Assert(package != null);
            }

            return package;
        }

        private static bool ContainsSource(IPackageSourceProvider provider, string source)
        {
            return provider.GetEnabledPackageSources().Any(p => p.Source.Equals(source, StringComparison.OrdinalIgnoreCase));
        }

        private void DisableSourceControlMode()
        {
            // get the settings for this solution
            var nugetFolder = Path.Combine(_solutionManager.SolutionDirectory, VsConstants.NuGetSolutionSettingsFolder);
            var settings = new Settings(_fileSystemProvider.GetFileSystem(nugetFolder));
            settings.DisableSourceControlMode();
        }

        private void OnProjectAdded(object sender, ProjectEventArgs e)
        {
            if (IsCurrentSolutionEnabledForRestore)
            {
                EnablePackageRestore(e.Project, _packageManagerFactory.CreatePackageManager());
            }

            CheckForMissingPackages();
        }

        private void OnPackageReferenceAdded(IVsPackageMetadata metadata)
        {
            if (IsCurrentSolutionEnabledForRestore)
            {
                var packageMetadata = (VsPackageMetadata)metadata;
                var fileSystem = packageMetadata.FileSystem as IVsProjectSystem;
                if (fileSystem != null)
                {
                    var project = _solutionManager.GetProject(fileSystem.UniqueName);
                    if (project != null)
                    {
                        // in this case, we know that this project has at least one nuget package,
                        // so enable package restore straight away
                        EnablePackageRestore(project);
                    }
                }
            }
        }

        private void OnSolutionOpenedOrClosed(object sender, EventArgs e)
        {
            // We need to do the check even on Solution Closed because, let's say if the yellow Update bar
            // is showing and the user closes the solution; in that case, we want to hide the Update bar.
            CheckForMissingPackages();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't care about exception handling here.")]
        private bool CheckForMissingPackagesCore()
        {
            // this can happen during unit tests
            if (_packageManagerFactory == null)
            {
                return false;
            }

            try
            {
                IVsPackageManager packageManager = _packageManagerFactory.CreatePackageManager();
                IPackageRepository localRepository = packageManager.LocalRepository;
                var projectReferences = GetAllPackageReferences(packageManager);
                return projectReferences.Any(reference => !localRepository.Exists(reference.Id, reference.Version));
            }
            catch (Exception exception)
            {
                // if an exception happens during the check, assume no missing packages and move on.
                ExceptionHelper.WriteToActivityLog(exception);
                return false;
            }
        }

        /// <summary>
        /// Gets all package references in all projects of the current solution plus package 
        /// references specified in the solution packages.config
        /// </summary>
        private IEnumerable<PackageReference> GetAllPackageReferences(IVsPackageManager packageManager)
        {
            IEnumerable<PackageReference> projectReferences = from project in _solutionManager.GetProjects()
                                                              from reference in GetPackageReferences(packageManager.GetProjectManager(project))
                                                              select reference;

            var localRepository = packageManager.LocalRepository as SharedPackageRepository;
            if (localRepository != null)
            {
                IEnumerable<PackageReference> solutionReferences = localRepository.PackageReferenceFile.GetPackageReferences();
                return projectReferences.Concat(solutionReferences).Distinct();
            }

            return projectReferences.Distinct();
        }

        /// <summary>
        /// Gets the package references of the specified project.
        /// </summary>
        private IEnumerable<PackageReference> GetPackageReferences(IProjectManager projectManager)
        {
            var packageRepository = projectManager.LocalRepository as PackageReferenceRepository;
            if (packageRepository != null)
            {
                return packageRepository.ReferenceFile.GetPackageReferences();
            }

            return new PackageReference[0];
        }

        /// <summary>
        /// Invokes the action on the UI thread if one exists.
        /// </summary>
        private void InvokeOnUIThread(Action action)
        {
            if (Application.Current != null)
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(action);
                }
                catch (TaskCanceledException)
                {
                    // Thrown if VS is closed while the action is being processed.
                }
            }
            else
            {
                action();
            }
        }
    }
}