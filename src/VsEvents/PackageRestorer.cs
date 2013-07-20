using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;

namespace NuGet.VsEvents
{
    public sealed class PackageRestorer : IDisposable
    {
        private const string LogEntrySource = "NuGet PackageRestorer";        

        private DTE _dte;
        private OutputWindowPane _outputPane;
        private bool _outputOptOutMessage;

        // Indicates if there are missing packages.
        private bool _hasMissingPackages;

        // The value of the "MSBuild project build output verbosity" setting 
        // of VS. From 0 (quiet) to 4 (Diagnostic).
        private int _msBuildOutputVerbosity;

        // keeps a reference to BuildEvents so that our event handler
        // won't get disconnected.
        private BuildEvents _buildEvents;

        private SolutionEvents _solutionEvents;
                
        private ErrorListProvider _errorListProvider;

        enum VerbosityLevel
        {
            Quiet = 0,
            Minimal = 1,
            Normal = 2,
            Detailed = 3,
            Diagnostic = 4
        };

        public PackageRestorer(DTE dte, IServiceProvider serviceProvider)
        {
            _dte = dte;
            _errorListProvider = new ErrorListProvider(serviceProvider);
            _buildEvents = dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
            _solutionEvents = dte.Events.SolutionEvents;
            _solutionEvents.AfterClosing += SolutionEvents_AfterClosing;            

            // get the "Build" output window pane
            var dte2 = (DTE2)dte;
            var buildWindowPaneGuid = VSConstants.BuildOutput.ToString("B");
            foreach (OutputWindowPane pane in dte2.ToolWindows.OutputWindow.OutputWindowPanes)
            {
                if (String.Equals(pane.Guid, buildWindowPaneGuid, StringComparison.OrdinalIgnoreCase))
                {
                    _outputPane = pane;
                    break;
                }
            }
        }

        private void SolutionEvents_AfterClosing()
        {
            _errorListProvider.Tasks.Clear();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to log an exception as a warning and move on")]
        private void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            try
            {
                if (UsingOldPackageRestore(_dte.Solution))
                {
                    return;
                }

                if (!VsUtility.PackagesConfigExists(_dte.Solution))
                {
                    return;
                }

                _outputOptOutMessage = true;
                _hasMissingPackages = false;
                RestorePackagesOrCheckForMissingPackages();
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.ErrorOccurredRestoringPackages, ex.ToString());
                WriteLine(VerbosityLevel.Quiet, message);
                ActivityLog.LogError(LogEntrySource, message);
            }
        }

        private void RestorePackagesOrCheckForMissingPackages()
        {
            var waitDialogFactory = ServiceLocator.GetGlobalService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>();
            IVsThreadedWaitDialog2 waitDialog;
            waitDialogFactory.CreateInstance(out waitDialog);

            waitDialog.StartWaitDialog(
                VsResources.DialogTitle,
                Resources.RestoringPackages,
                String.Empty,
                varStatusBmpAnim: null,
                szStatusBarText: null,
                iDelayToShowDialog: 0,
                fIsCancelable: true,
                fShowMarqueeProgress: true);

            try
            {
                _errorListProvider.Tasks.Clear();
                if (IsConsentGranted())
                {
                    RestorePackages(waitDialog);
                }
                else
                {
                    CheckForMissingPackages(waitDialog);
                }
            }
            finally
            {
                int canceled;
                waitDialog.EndWaitDialog(out canceled);
            }
        }

        private void RestorePackages(IVsThreadedWaitDialog2 waitDialog)
        {
            bool canceled;            
            _msBuildOutputVerbosity = GetMSBuildOutputVerbositySetting(_dte);
            WriteLine(VerbosityLevel.Minimal, Resources.PackageRestoreStarted);
            PackageRestore(_dte.Solution, waitDialog);
            foreach (Project project in _dte.Solution.Projects)
            {
                waitDialog.HasCanceled(out canceled);
                if (canceled)
                {
                    break;
                }

                if (project.IsUnloaded() || project.IsNuGetInUse())
                {
                    PackageRestore(project, waitDialog);
                }
            }

            waitDialog.HasCanceled(out canceled);
            if (canceled)
            {
                WriteLine(VerbosityLevel.Minimal, Resources.PackageRestoreCanceled);
            }
            else
            {
                if (!_hasMissingPackages)
                {
                    WriteLine(VerbosityLevel.Minimal, Resources.NothingToRestore);
                }
                WriteLine(VerbosityLevel.Minimal, Resources.PackageRestoreFinished);
            }
        }

        /// <summary>
        /// Checks if there are missing packages that should be restored. If so, a warning will 
        /// be added to the error list.
        /// </summary>
        /// <param name="waitDialog">The wait dialog.</param>
        private void CheckForMissingPackages(IVsThreadedWaitDialog2 waitDialog)
        {
            var missingPackages = new List<PackageReference>();
            var repoSettings = ServiceLocator.GetInstance<IRepositorySettings>();
            var fileSystem = new PhysicalFileSystem(repoSettings.RepositoryPath);

            missingPackages.AddRange(GetMissingPackages(_dte.Solution, fileSystem));
            foreach (Project project in _dte.Solution.Projects)
            {
                bool canceled;
                waitDialog.HasCanceled(out canceled);
                if (canceled)
                {
                    return;
                }

                if (project.IsUnloaded())
                {
                    var projectFullPath = project.GetFullPath();
                    if (File.Exists(projectFullPath))
                    {
                        var packageReferenceFileFullPath = Path.Combine(
                            Path.GetDirectoryName(projectFullPath),
                            VsUtility.PackageReferenceFile);
                        missingPackages.AddRange(GetMissingPackages(packageReferenceFileFullPath, fileSystem));
                    }
                }
                else
                {
                    if (project.IsNuGetInUse())
                    {
                        missingPackages.AddRange(GetMissingPackages(project, fileSystem));
                    }
                }
            }

            if (missingPackages.Count > 0)
            {
                var errorText = String.Format(CultureInfo.CurrentCulture, 
                    Resources.PackageNotRestoredBecauseOfNoConsent,
                    String.Join(", ", missingPackages.Select(p => p.ToString())));
                VsUtility.ShowError(_errorListProvider, TaskErrorCategory.Error, TaskPriority.Normal, errorText, hierarchyItem: null);
            }
        }

        /// <summary>
        /// Gets the list of missing solution level packages of the solution.
        /// </summary>
        /// <param name="solution">The solution to check.</param>
        /// <param name="fileSystem">The file sytem of the local repository.</param>
        /// <returns>The list of missing packages.</returns>
        private static IEnumerable<PackageReference> GetMissingPackages(Solution solution, IFileSystem fileSystem)
        {
            var packageReferenceFileFullPath = Path.Combine(
                VsUtility.GetNuGetSolutionFolder(solution),
                VsUtility.PackageReferenceFile);
            return GetMissingPackages(packageReferenceFileFullPath, fileSystem);
        }

        /// <summary>
        /// Gets the list of missing packages of the project.
        /// </summary>
        /// <param name="project">The project to check.</param>
        /// /// <param name="fileSystem">The file sytem of the local repository.</param>
        /// <returns>The list of missing packages.</returns>
        private static IEnumerable<PackageReference> GetMissingPackages(Project project, IFileSystem fileSystem)
        {
            var projectFullPath = VsUtility.GetFullPath(project);            
            var packageReferenceFileFullPath = Path.Combine(
                    Path.GetDirectoryName(projectFullPath),
                    VsUtility.PackageReferenceFile);
            return GetMissingPackages(packageReferenceFileFullPath, fileSystem);
        }

        /// <summary>
        /// Gets the list of missing packages listed in the package reference file.
        /// </summary>
        /// <param name="packageReferenceFileFullPath">The full path of the package reference file.</param>
        /// <param name="fileSystem">The file system of the local repository</param>
        /// <returns>The list of missing packages.</returns>
        private static IEnumerable<PackageReference> GetMissingPackages(string packageReferenceFileFullPath, IFileSystem fileSystem)
        {
            var packageReferenceFile = new PackageReferenceFile(packageReferenceFileFullPath);
            return packageReferenceFile.GetPackageReferences()
                .Where(package => !IsPackageInstalled(fileSystem, package.Id, package.Version));
        }

        /// <summary>
        /// Returns true if the package restore user consent is granted.
        /// </summary>
        /// <returns>True if the package restore user consent is granted.</returns>
        private static bool IsConsentGranted()
        {
            var settings = ServiceLocator.GetInstance<ISettings>();
            var packageRestoreConsent = new PackageRestoreConsent(settings);
            return packageRestoreConsent.IsGranted;
        }

        /// <summary>
        /// Returns true if the solution is using the old style package restore.
        /// </summary>
        /// <param name="solution">The solution to check.</param>
        /// <returns>True if the solution is using the old style package restore.</returns>
        private static bool UsingOldPackageRestore(Solution solution)
        {
            var nugetSolutionFolder = VsUtility.GetNuGetSolutionFolder(solution);
            return File.Exists(Path.Combine(nugetSolutionFolder, "nuget.targets"));
        }

        /// <summary>
        /// Restores NuGet packages for the given project.
        /// </summary>
        /// <param name="project">The project whose packages will be restored.</param>
        /// <param name="waitDialog">The wait dialog.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to log an exception as a warning and move on")]
        private void PackageRestore(Project project, IVsThreadedWaitDialog2 waitDialog)
        {
            bool canceled;
            waitDialog.HasCanceled(out canceled);
            if (canceled)
            {
                return;
            }

            var repoSettings = ServiceLocator.GetInstance<IRepositorySettings>();
            var fileSystem = new PhysicalFileSystem(repoSettings.RepositoryPath);
            var projectFullPath = VsUtility.GetFullPath(project);
            
            WriteLine(VerbosityLevel.Normal, Resources.RestoringPackagesForProject, projectFullPath);

            try
            {
                var packageReferenceFileFullPath = VsUtility.GetPackageReferenceFileFullPath(project);
                RestorePackages(packageReferenceFileFullPath, fileSystem, waitDialog);
            }
            catch (Exception ex)
            {
                var message = String.Format(CultureInfo.CurrentCulture, Resources.PackageRestoreFailedForProject, projectFullPath, ex.Message);
                WriteLine(VerbosityLevel.Quiet, message);
                ActivityLog.LogError(LogEntrySource, message);
            }
            finally
            {
                WriteLine(VerbosityLevel.Normal, Resources.PackageRestoreFinishedForProject, projectFullPath);
            }
        }

        /// <summary>
        /// Restores the given package into the packages folder represented by 'fileSystem'.
        /// </summary>
        /// <param name="package">The package to be restored.</param>
        /// <param name="fileSystem">The file system representing the packages folder.</param>
        private void RestorePackage(PackageReference package, IFileSystem fileSystem)
        {
            WriteLine(VerbosityLevel.Normal, Resources.RestoringPackage, package);
            
            if (IsPackageInstalled(fileSystem, package.Id, package.Version))
            {
                WriteLine(VerbosityLevel.Normal, Resources.SkippingInstalledPackage, package);
                return;
            }

            _hasMissingPackages = true;
            if (_outputOptOutMessage)
            {
                WriteLine(VerbosityLevel.Quiet, Resources.PackageRestoreOptOutMessage);
                _outputOptOutMessage = false;
            }

            IVsPackageManagerFactory packageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            var packageManager = packageManagerFactory.CreatePackageManager();
            using (packageManager.SourceRepository.StartOperation(RepositoryOperationNames.Restore, package.Id))
            {
                var resolvedPackage = PackageHelper.ResolvePackage(
                    packageManager.SourceRepository, package.Id, package.Version);
                NuGet.Common.PackageExtractor.InstallPackage(packageManager, resolvedPackage);
                WriteLine(VerbosityLevel.Normal, Resources.PackageRestored, resolvedPackage);
            }
        }

        /// <summary>
        /// Returns true if the package is already installed in the local repository.
        /// </summary>
        /// <param name="fileSystem">The file system of the local repository.</param>
        /// <param name="packageId">The package id.</param>
        /// <param name="version">The package version</param>
        /// <returns>True if the package is installed in the local repository.</returns>
        private static bool IsPackageInstalled(IFileSystem fileSystem, string packageId, SemanticVersion version)
        {
            if (version != null)
            {
                // If we know exactly what package to lookup, check if it's already installed locally. 
                // We'll do this by checking if the package directory exists on disk.
                var localRepository = new LocalPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem);
                var packagePaths = localRepository.GetPackageLookupPaths(packageId, version);
                return packagePaths.Any(fileSystem.FileExists);
            }
            return false;
        }

        /// <summary>
        /// Restores packages listed in the <paramref name="packageReferenceFileFullPath"/> into the packages folder
        /// represented by <paramref name="fileSystem"/>.
        /// </summary>
        /// <param name="packageReferenceFileFullPath">The package reference file full path.</param>
        /// <param name="fileSystem">The file system that represents the packages folder.</param>
        /// <param name="waitDialog">The wait dialog.</param>
        private void RestorePackages(string packageReferenceFileFullPath, IFileSystem fileSystem, IVsThreadedWaitDialog2 waitDialog)
        {
            var packageReferenceFile = new PackageReferenceFile(packageReferenceFileFullPath);
            var packages = packageReferenceFile.GetPackageReferences().ToList();
            int currentCount = 1;
            int totalCount = packages.Count;
            foreach (var package in packages)
            {
                bool canceled;
                waitDialog.UpdateProgress(
                    String.Format(CultureInfo.CurrentCulture, Resources.RestoringPackagesListedInFile, packageReferenceFileFullPath),
                    String.Format(CultureInfo.CurrentCulture, Resources.RestoringPackage, package.ToString()),
                    szStatusBarText: null,
                    iCurrentStep: currentCount,
                    iTotalSteps: totalCount,
                    fDisableCancel: false,
                    pfCanceled: out canceled);
                if (canceled)
                {
                    return;
                }

                RestorePackage(package, fileSystem);
                ++currentCount;
            }
        }

        /// <summary>
        /// Restores NuGet packages for the given solution.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="waitDialog">The wait dialog.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to log an exception as a warning and move on")] 
        private void PackageRestore(Solution solution, IVsThreadedWaitDialog2 waitDialog)
        {
            WriteLine(VerbosityLevel.Normal, Resources.RestoringPackagesForSolution, solution.FullName);            
            var repoSettings = ServiceLocator.GetInstance<IRepositorySettings>();
            var fileSystem = new PhysicalFileSystem(repoSettings.RepositoryPath);

            try
            {
                var packageReferenceFileFullPath = Path.Combine(
                    VsUtility.GetNuGetSolutionFolder(solution),
                    VsUtility.PackageReferenceFile);
                RestorePackages(packageReferenceFileFullPath, fileSystem, waitDialog);
            }
            catch (Exception ex)
            {
                var message = String.Format(CultureInfo.CurrentCulture, Resources.PackageRestoreFailedForSolution, solution.FullName, ex.Message);
                WriteLine(VerbosityLevel.Quiet, message);
                ActivityLog.LogError(LogEntrySource, message);
            }
            finally
            {
                WriteLine(VerbosityLevel.Normal, Resources.PackageRestoreFinishedForSolution, solution.FullName);
            }
        }

        /// <summary>
        /// Outputs a message to the debug output pane, if the VS MSBuildOutputVerbosity
        /// setting value is greater than or equal to the given verbosity. So if verbosity is 0,
        /// it means the message is always written to the output pane.
        /// </summary>
        /// <param name="verbosity">The verbosity level.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">An array of objects to write using format. </param>
        private void WriteLine(VerbosityLevel verbosity, string format, params object[] args)
        {
            if (_outputPane == null)
            {
                return;
            }

            if (_msBuildOutputVerbosity >= (int)verbosity)
            {
                var msg = string.Format(CultureInfo.CurrentCulture, format, args);
                _outputPane.OutputString(msg);
                _outputPane.OutputString(Environment.NewLine);
            }
        }

        /// <summary>
        /// Returns the value of the VisualStudio MSBuildOutputVerbosity setting.
        /// </summary>
        /// <param name="dte">The VisualStudio instance.</param>
        /// <remarks>
        /// 0 is Quiet, while 4 is diagnostic.
        /// </remarks>
        private static int GetMSBuildOutputVerbositySetting(DTE dte)
        {
            var properties = dte.get_Properties("Environment", "ProjectsAndSolution");
            var value = properties.Item("MSBuildOutputVerbosity").Value;
            if (value is int)
            {
                return (int)value;
            }
            else
            {
                return 0;
            }
        }

        public void Dispose()
        {
            _errorListProvider.Dispose();
            _buildEvents.OnBuildBegin -= BuildEvents_OnBuildBegin;
            _solutionEvents.AfterClosing -= SolutionEvents_AfterClosing;
        }
    }
}
