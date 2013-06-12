using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;

namespace NuGet.VsEvents
{
    public class PackageRestorer
    {
        private const string PackageReferenceFile = "packages.config";
        private const string NuGetSolutionSettingsFolder = ".nuget";
        private const string LogEntrySource = "NuGet PackageRestorer";        

        private DTE _dte;
        private OutputWindowPane _outputPane;

        // The value of the "MSBuild project build output verbosity" setting 
        // of VS. From 0 (quiet) to 4 (Diagnostic).
        private int _msBuildOutputVerbosity;

        // keeps a reference to BuildEvents so that our event handler
        // won't get disconnected.
        private BuildEvents _buildEvents;

        private bool _isConsentGranted;

        enum VerbosityLevel
        {
            Quiet = 0,
            Minimal = 1,
            Normal = 2,
            Detailed = 3,
            Diagnostic = 4
        };

        public void Initialize(DTE dte)
        {
            _dte = dte;
            _buildEvents = dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;

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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to log an exception as a warning and move on")]
        private void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            try
            {
                if (UsingOldPackageRestore(_dte.Solution))
                {
                    return;
                }

                if (!PackagesConfigExists(_dte.Solution))
                {
                    return;
                }

                RestorePackages();                
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.ErrorOccurredRestoringPackages, ex.ToString());
                WriteLine(VerbosityLevel.Quiet, message);
                ActivityLog.LogError(LogEntrySource, message);
            }
        }

        private void RestorePackages()
        {
            _isConsentGranted = IsConsentGranted();
            _msBuildOutputVerbosity = GetMSBuildOutputVerbositySetting(_dte);
            WriteLine(VerbosityLevel.Minimal, Resources.PackageRestoreStarted);
            PackageRestore(_dte.Solution);
            foreach (Project project in _dte.Solution.Projects)
            {
                if (VsUtility.IsSupported(project) &&
                    project.ContainsFile(PackageReferenceFile))
                {
                    PackageRestore(project);
                }
            }
            WriteLine(VerbosityLevel.Minimal, Resources.PackageRestoreFinished);
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
            var nugetSolutionFolder = GetNuGetSolutionFolder(solution);
            return File.Exists(Path.Combine(nugetSolutionFolder, "nuget.targets"));
        }

        /// <summary>
        /// Returns true if the solution or one of its project has the packages.config file.
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        private static bool PackagesConfigExists(Solution solution)
        {
            var packageReferenceFileName = Path.Combine(
                    GetNuGetSolutionFolder(solution),
                    PackageReferenceFile);
            if (File.Exists(packageReferenceFileName))
            {
                return true;
            }

            foreach (Project project in solution.Projects)
            {
                var projectFullPath = VsUtility.GetFullPath(project);
                packageReferenceFileName = Path.Combine(
                    Path.GetDirectoryName(projectFullPath),
                    PackageReferenceFile);

                // Here we just check if the packages.config file exists instead of checking
                // if project.ContainsFile(packageReferenceFileName) because that will
                // cause NuGet.VisualStudio.dll to get loaded.
                if (VsUtility.IsSupported(project) && File.Exists(packageReferenceFileName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Restores NuGet packages for the given project.
        /// </summary>
        /// <param name="project">The project whose packages will be restored.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to log an exception as a warning and move on")]
        private void PackageRestore(Project project)
        {   
            var repoSettings = ServiceLocator.GetInstance<IRepositorySettings>();
            var fileSystem = new PhysicalFileSystem(repoSettings.RepositoryPath);
            var projectFullPath = VsUtility.GetFullPath(project);
            WriteLine(VerbosityLevel.Normal, Resources.RestoringPackagesOfProject, projectFullPath);

            try
            {
                var packageReferenceFileName = Path.Combine(
                    Path.GetDirectoryName(projectFullPath),
                    PackageReferenceFile);
                RestorePackages(packageReferenceFileName, fileSystem);
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

            IVsPackageManagerFactory packageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            var packageManager = packageManagerFactory.CreatePackageManager();

            if (IsPackageInstalled(packageManager.LocalRepository, fileSystem, package.Id, package.Version))
            {
                WriteLine(VerbosityLevel.Normal, Resources.SkipInstalledPackage, package);
                return;
            }

            if (_isConsentGranted)
            {
                using (packageManager.SourceRepository.StartOperation(RepositoryOperationNames.Restore, package.Id))
                {
                    var resolvedPackage = PackageHelper.ResolvePackage(
                        packageManager.SourceRepository, package.Id, package.Version);
                    NuGet.Common.PackageExtractor.InstallPackage(packageManager, resolvedPackage);
                    WriteLine(VerbosityLevel.Normal, Resources.PackageRestored, resolvedPackage);
                }
            }
            else
            {
                WriteLine(VerbosityLevel.Quiet, Resources.PackageNotRestoredBecauseOfNoConsent, package);
            }
        }

        /// <summary>
        /// Returns true if the package is already installed in the local repository.
        /// </summary>
        /// <param name="repository">The local repository.</param>
        /// <param name="fileSystem">The file system of the local repository.</param>
        /// <param name="packageId">The package id.</param>
        /// <param name="version">The package version</param>
        /// <returns>True if the package is installed in the local repository.</returns>
        private static bool IsPackageInstalled(IPackageRepository repository, IFileSystem fileSystem,
            string packageId, SemanticVersion version)
        {
            if (version != null)
            {
                // If we know exactly what package to lookup, check if it's already installed locally. 
                // We'll do this by checking if the package directory exists on disk.
                var localRepository = repository as LocalPackageRepository;
                var packagePaths = localRepository.GetPackageLookupPaths(packageId, version);
                return packagePaths.Any(fileSystem.FileExists);
            }
            return false;
        }

        /// <summary>
        /// Restores packages listed in the <paramref name="packageReferenceFileName"/> into the packages folder
        /// represented by <paramref name="fileSystem"/>.
        /// </summary>
        /// <param name="packageReferenceFileName">The package reference file name.</param>
        /// <param name="fileSystem">The file system that represents the packages folder.</param>
        private void RestorePackages(string packageReferenceFileName, IFileSystem fileSystem)
        {
            var packageReferenceFile = new PackageReferenceFile(packageReferenceFileName);
            var packageReferences = packageReferenceFile.GetPackageReferences().ToList();
            foreach (var package in packageReferences)
            {
                RestorePackage(package, fileSystem);
            }
        }

        /// <summary>
        /// Restores NuGet packages for the given solution.
        /// </summary>
        /// <param name="solution">The solution.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to log an exception as a warning and move on")] 
        private void PackageRestore(Solution solution)
        {
            WriteLine(VerbosityLevel.Normal, Resources.RestoringPackagesOfSolution, solution.FullName);            
            var repoSettings = ServiceLocator.GetInstance<IRepositorySettings>();
            var fileSystem = new PhysicalFileSystem(repoSettings.RepositoryPath);

            try
            {
                var packageReferenceFileName = Path.Combine(
                    GetNuGetSolutionFolder(solution),
                    PackageReferenceFile);
                RestorePackages(packageReferenceFileName, fileSystem);
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

        private static string GetNuGetSolutionFolder(Solution solution)
        {
            var solutionDirectory = Path.GetDirectoryName(solution.FullName);
            return Path.Combine(solutionDirectory, NuGetSolutionSettingsFolder);
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
    }
}
