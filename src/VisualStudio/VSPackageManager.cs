using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using EnvDTE;

#if VS14
using Microsoft.VisualStudio.ProjectSystem.Interop;
#endif

using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    public class VsPackageManager : PackageManager, IVsPackageManager
    {
        private readonly ISharedPackageRepository _sharedRepository;

        private readonly IDictionary<string, IProjectManager> _projects;
        private readonly ISolutionManager _solutionManager;
        protected readonly IFileSystemProvider _fileSystemProvider;
        private readonly IDeleteOnRestartManager _deleteOnRestartManager;
        private readonly VsPackageInstallerEvents _packageEvents;
        private readonly IVsFrameworkMultiTargeting _frameworkMultiTargeting;
        private bool _repositoryOperationPending;

        public VsPackageManager(ISolutionManager solutionManager,
                IPackageRepository sourceRepository,
                IFileSystemProvider fileSystemProvider,
                IFileSystem fileSystem,
                ISharedPackageRepository sharedRepository,
                IDeleteOnRestartManager deleteOnRestartManager,
                VsPackageInstallerEvents packageEvents,
                IVsFrameworkMultiTargeting frameworkMultiTargeting = null)
            : base(sourceRepository, new DefaultPackagePathResolver(fileSystem), fileSystem, sharedRepository)
        {
            _solutionManager = solutionManager;
            _sharedRepository = sharedRepository;
            _packageEvents = packageEvents;
            _fileSystemProvider = fileSystemProvider;
            _deleteOnRestartManager = deleteOnRestartManager;
            _frameworkMultiTargeting = frameworkMultiTargeting;
            _projects = new Dictionary<string, IProjectManager>(StringComparer.OrdinalIgnoreCase);
        }

        public ISolutionManager SolutionManager
        {
            get
            {
                return _solutionManager;
            }
        }

        internal void EnsureCached(Project project)
        {
            string projectUniqueName = project.GetUniqueName();
            if (_projects.ContainsKey(projectUniqueName))
            {
                return;
            }

            _projects[projectUniqueName] = CreateProjectManager(project);
        }

        public VsPackageInstallerEvents PackageEvents
        {
            get
            {
                return _packageEvents;
            }
        }

        public virtual IProjectManager GetProjectManager(Project project)
        {
            EnsureCached(project);
            IProjectManager projectManager;
            bool projectExists = _projects.TryGetValue(project.GetUniqueName(), out projectManager);
            Debug.Assert(projectExists, "Unknown project");
            return projectManager;
        }

        private IProjectManager CreateProjectManager(Project project)
        {
            // Create the project system
            IProjectSystem projectSystem = VsProjectSystemFactory.CreateProjectSystem(project, _fileSystemProvider);

#if VS14
            if (projectSystem is INuGetPackageManager)
            {
                var nugetAwareRepo = new NuGetAwareProjectPackageRepository((INuGetPackageManager)projectSystem, _sharedRepository);
                return new ProjectManager(this, PathResolver, projectSystem, nugetAwareRepo);
            }
#endif

            PackageReferenceRepository repository = new PackageReferenceRepository(projectSystem, project.GetProperName(), _sharedRepository);

            // Ensure the logger is null while registering the repository
            FileSystem.Logger = null;
            Logger = null;

            // Ensure that this repository is registered with the shared repository if it needs to be
            if (repository != null)
            {
                repository.RegisterIfNecessary();
            }

            var projectManager = new VsProjectManager(this, PathResolver, projectSystem, repository);

            // The package reference repository also provides constraints for packages (via the allowedVersions attribute)
            projectManager.ConstraintProvider = repository;
            return projectManager;
        }

        protected override void ExecuteUninstall(IPackage package)
        {
            // Check if the package is in use before removing it
            if (!_sharedRepository.IsReferenced(package.Id, package.Version))
            {
                base.ExecuteUninstall(package);
            }
        }

        public IPackage FindLocalPackage(IProjectManager projectManager,
                                          string packageId,
                                          SemanticVersion version,
                                          Func<IProjectManager, IList<IPackage>, Exception> getAmbiguousMatchException)
        {
            IPackage package = null;
            bool existsInProject = false;
            bool appliesToProject = false;

            if (projectManager != null)
            {
                // Try the project repository first
                package = projectManager.LocalRepository.FindPackage(packageId, version);

                existsInProject = package != null;
            }

            // Fallback to the solution repository (it might be a solution only package)
            if (package == null)
            {
                if (version != null)
                {
                    // Get the exact package
                    package = LocalRepository.FindPackage(packageId, version);
                }
                else
                {
                    // Get all packages by this name to see if we find an ambiguous match
                    var packages = LocalRepository.FindPackagesById(packageId).ToList();
                    if (packages.Count > 1)
                    {
                        throw getAmbiguousMatchException(projectManager, packages);
                    }

                    // Pick the only one of default if none match
                    package = packages.SingleOrDefault();
                }
            }

            // Can't find the package in the solution or in the project then fail
            if (package == null)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.UnknownPackage, packageId));
            }

#if VS14
            bool isNuGetAwareProjectSystem = (projectManager != null && projectManager.Project is NuGetAwareProjectSystem);
#else
            bool isNuGetAwareProjectSystem = false;
#endif
            appliesToProject = isNuGetAwareProjectSystem || IsProjectLevel(package);

            if (appliesToProject)
            {
                if (!existsInProject)
                {
                    if (_sharedRepository.IsReferenced(package.Id, package.Version))
                    {
                        // If the package doesn't exist in the project and is referenced by other projects
                        // then fail.
                        if (projectManager != null)
                        {
                            if (version == null)
                            {
                                throw new InvalidOperationException(
                                        String.Format(CultureInfo.CurrentCulture,
                                        VsResources.UnknownPackageInProject,
                                        package.Id,
                                        projectManager.Project.ProjectName));
                            }

                            throw new InvalidOperationException(
                                    String.Format(CultureInfo.CurrentCulture,
                                    VsResources.UnknownPackageInProject,
                                    package.GetFullName(),
                                    projectManager.Project.ProjectName));
                        }
                    }
                    else
                    {
                        // The operation applies to solution level since it's not installed in the current project
                        // but it is installed in some other project
                        appliesToProject = false;
                    }
                }
            }

            // Can't have a project level operation if no project was specified
            if (appliesToProject && projectManager == null)
            {
                throw new InvalidOperationException(VsResources.ProjectNotSpecified);
            }

            return package;
        }

        public IPackage FindLocalPackage(string packageId, out bool appliesToProject)
        {
            // It doesn't matter if there are multiple versions of the package installed at solution level,
            // we just want to know that one exists.
            var packages = LocalRepository.FindPackagesById(packageId).OrderByDescending(p => p.Version).ToList();

            // Can't find the package in the solution.
            if (!packages.Any())
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.UnknownPackage, packageId));
            }

            foreach (IPackage package in packages)
            {
                appliesToProject = IsProjectLevel(package);

                if (!appliesToProject)
                {
                    if (packages.Count > 1)
                    {
                        throw CreateAmbiguousUpdateException(projectManager: null, packages: packages);
                    }
                }
                else if (!_sharedRepository.IsReferenced(package.Id, package.Version))
                {
                    Logger.Log(MessageLevel.Warning, String.Format(CultureInfo.CurrentCulture,
                        VsResources.Warning_PackageNotReferencedByAnyProject, package.Id, package.Version));

                    // Try next package
                    continue;
                }

                // Found a package with package Id as 'packageId' which is installed in at least 1 project
                return package;
            }

            // There are one or more packages with package Id as 'packageId'
            // BUT, none of them is installed in a project
            // it's probably a borked install.
            throw new PackageNotInstalledException(
                String.Format(CultureInfo.CurrentCulture,
                VsResources.PackageNotInstalledInAnyProject, packageId));
        }

        public Exception CreateAmbiguousUpdateException(IProjectManager projectManager, IList<IPackage> packages)
        {
            if (projectManager != null && packages.Any(IsProjectLevel))
            {
                return new InvalidOperationException(
                                    String.Format(CultureInfo.CurrentCulture,
                                    VsResources.UnknownPackageInProject,
                                    packages[0].Id,
                                    projectManager.Project.ProjectName));
            }

            return new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.AmbiguousUpdate,
                    packages[0].Id));
        }

        public Exception CreateAmbiguousUninstallException(IProjectManager projectManager, IList<IPackage> packages)
        {
            if (projectManager != null && packages.Any(IsProjectLevel))
            {
                return new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.AmbiguousProjectLevelUninstal,
                    packages[0].Id,
                    projectManager.Project.ProjectName));
            }

            return new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.AmbiguousUninstall,
                    packages[0].Id));
        }

        public Project GetProject(IProjectManager projectManager)
        {
            // We only support project systems that implement IVsProjectSystem
            var vsProjectSystem = projectManager.Project as IVsProjectSystem;
            if (vsProjectSystem == null)
            {
                return null;
            }

            // Find the project by it's unique name
            return _solutionManager.GetProject(vsProjectSystem.UniqueName);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "If we failed to add binding redirects we don't want it to stop the install/update.")]
        public override void AddBindingRedirects(IProjectManager projectManager)
        {
            // Find the project by it's unique name
            Project project = GetProject(projectManager);

            // If we can't find the project or it doesn't support binding redirects then don't add any redirects
            if (project == null || !project.SupportsBindingRedirects())
            {
                return;
            }

            try
            {
                RuntimeHelpers.AddBindingRedirects(_solutionManager, project, _fileSystemProvider, _frameworkMultiTargeting);
            }
            catch (Exception e)
            {
                // If there was an error adding binding redirects then print a warning and continue
                Logger.Log(MessageLevel.Warning, String.Format(CultureInfo.CurrentCulture, VsResources.Warning_FailedToAddBindingRedirects, projectManager.Project.ProjectName, e.Message));
            }
        }

        protected override void OnUninstalled(PackageOperationEventArgs e)
        {
            base.OnUninstalled(e);

            PackageEvents.NotifyUninstalled(e);
            _deleteOnRestartManager.MarkPackageDirectoryForDeletion(e.Package);
        }

        private IDisposable StartInstallOperation(string packageId, string packageVersion)
        {
            return StartOperation(RepositoryOperationNames.Install, packageId, packageVersion);
        }

        private IDisposable StartUpdateOperation(string packageId, string packageVersion)
        {
            return StartOperation(RepositoryOperationNames.Update, packageId, packageVersion);
        }

        private IDisposable StartReinstallOperation(string packageId, string packageVersion)
        {
            return StartOperation(RepositoryOperationNames.Reinstall, packageId, packageVersion);
        }

        private IDisposable StartOperation(string operation, string packageId, string mainPackageVersion)
        {
            // If there's a pending operation, don't allow another one to start.
            // This is for the Reinstall case. Because Reinstall just means
            // uninstalling and installing, we don't want the child install operation
            // to override Reinstall value.
            if (_repositoryOperationPending)
            {
                return DisposableAction.NoOp;
            }

            _repositoryOperationPending = true;

            return DisposableAction.All(
                SourceRepository.StartOperation(operation, packageId, mainPackageVersion),
                new DisposableAction(() => _repositoryOperationPending = false));
        }

        protected override void OnInstalling(PackageOperationEventArgs e)
        {
            PackageEvents.NotifyInstalling(e);
            base.OnInstalling(e);
        }

        protected override void OnInstalled(PackageOperationEventArgs e)
        {
            base.OnInstalled(e);
            PackageEvents.NotifyInstalled(e);
        }

        protected override void OnUninstalling(PackageOperationEventArgs e)
        {
            PackageEvents.NotifyUninstalling(e);
            base.OnUninstalling(e);
        }

        public override IPackage LocatePackageToUninstall(IProjectManager projectManager, string id, SemanticVersion version)
        {
            return FindLocalPackage(
                projectManager,
                id,
                version,
                CreateAmbiguousUninstallException);
        }
    }
}