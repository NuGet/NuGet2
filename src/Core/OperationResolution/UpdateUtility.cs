using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Resolver;
using NuGet.Resources;
using NuGet.VisualStudio.Resources;

namespace NuGet
{
    public class UpdateUtility
    {
        public OperationResolver Resolver { get; private set; }
        public bool Safe { get; set; }
        public ILogger Logger { get; set; }
        public bool AllowPrereleaseVersions { get; set; }

        public UpdateUtility(OperationResolver resolver)
        {
            Resolver = resolver;
            Logger = NullLogger.Instance;
        }

        public IEnumerable<Operation> ResolveOperationsForUpdate(
            string id, 
            SemanticVersion version,
            IEnumerable<IProjectManager> projectManagers, bool projectNameSpecified)
        {
            if (string.IsNullOrEmpty(id))
            {
                return ResolveOperationsToUpdateAllPackages(projectManagers);
            }
            else
            {
                return ResolveOperationsToUpdateOnePackage(id, version, projectManagers, projectNameSpecified);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "NuGet.ILogger.Log(NuGet.MessageLevel,System.String,System.Object[])")]
        IEnumerable<Operation> ResolveOperationsToUpdateAllPackages(IEnumerable<IProjectManager> projectManagers)
        {
            // BUGBUG: TargetFramework should be passed for more efficient package walking
            var packageSorter = new PackageSorter(targetFramework: null);
            // Get the packages in reverse dependency order then run update on each one i.e. if A -> B run Update(A) then Update(B)
            var packages = packageSorter.GetPackagesByDependencyOrder(Resolver.PackageManager.LocalRepository).Reverse();

            var projectOperations = new List<Operation>();
            foreach (var projectManager in projectManagers.Select(p => new VirtualProjectManager(p)))
            {
                var operationsInOneProject = new List<Operation>();
                foreach (var package in packages)
                {
                    var ops = GetProjectOperationsForUpdate(
                        package.Id,
                        null,
                        new[] { projectManager });
                    operationsInOneProject.AddRange(ops);
                }

                projectOperations.AddRange(operationsInOneProject);
            }

            var operations = Resolver.ResolveFinalOperations(projectOperations);
            return operations;
        }

        // Get the list of project operations needed to update package (id, version)
        private List<Operation> GetProjectOperationsForUpdate(
            string id,
            SemanticVersion version,
            IEnumerable<VirtualProjectManager> projectManagers)
        {
            var projectOperations = new List<Operation>();

            if (!Safe)
            {
                // Update to latest version                
                foreach (var projectManager in projectManagers)
                {
                    var ops = GetProjectOperationsForUpdate(
                        id,
                        version,
                        version != null,
                        projectManager);

                    projectOperations.AddRange(ops);
                }
            }
            else
            {
                // safe update
                foreach (var projectManager in projectManagers)
                {
                    IPackage installedPackage = projectManager.LocalRepository.FindPackage(id);
                    if (installedPackage == null)
                    {
                        continue;
                    }

                    var safeRange = VersionUtility.GetSafeRange(installedPackage.Version);
                    var package = Resolver.PackageManager.SourceRepository.FindPackage(
                        id,
                        safeRange,
                        projectManager.ProjectManager.ConstraintProvider,
                        AllowPrereleaseVersions,
                        allowUnlisted: false);

                    var ops = Resolver.ResolveProjectOperations(
                        UserOperation.Install,
                        package,
                        projectManager);
                    projectOperations.AddRange(ops);
                }
            }

            return projectOperations;
        }

        // Unsafe update
        IList<Operation> GetProjectOperationsForUpdate(
            string id,
            SemanticVersion version,
            bool targetVersionSetExplicitly,
            VirtualProjectManager projectManager)
        {
            IList<Operation> ops = new List<Operation>();

            var oldPackage = projectManager.LocalRepository.FindPackage(id);
            if (oldPackage == null)
            {
                return ops;
            }

            Logger.Log(MessageLevel.Debug, NuGetResources.Debug_LookingForUpdates, id);

            var package = Resolver.PackageManager.SourceRepository.FindPackage(
                id, version,
                projectManager.ProjectManager.ConstraintProvider,
                AllowPrereleaseVersions,
                allowUnlisted: false);

            // the condition (allowPrereleaseVersions || targetVersionSetExplicitly || oldPackage.IsReleaseVersion() || !package.IsReleaseVersion() || oldPackage.Version < package.Version)
            // is to fix bug 1574. We want to do nothing if, let's say, you have package 2.0alpha installed, and you do:
            //      update-package
            // without specifying a version explicitly, and the feed only has version 1.0 as the latest stable version.
            if (package != null &&
                oldPackage.Version != package.Version &&
                (AllowPrereleaseVersions || targetVersionSetExplicitly || oldPackage.IsReleaseVersion() || !package.IsReleaseVersion() ||
                oldPackage.Version < package.Version))
            {
                Logger.Log(MessageLevel.Info, NuGetResources.Log_UpdatingPackages,
                    package.Id,
                    oldPackage.Version,
                    package.Version,
                    projectManager.ProjectManager.Project.ProjectName);

                ops = Resolver.ResolveProjectOperations(
                    UserOperation.Install,
                    package,
                    projectManager);
                return ops;
            }

            // Display message that no updates are available.
            IVersionSpec constraint = projectManager.ProjectManager.ConstraintProvider.GetConstraint(package.Id);
            if (constraint != null)
            {
                Logger.Log(MessageLevel.Info, NuGetResources.Log_ApplyingConstraints, package.Id, VersionUtility.PrettyPrint(constraint),
                    projectManager.ProjectManager.ConstraintProvider.Source);
            }

            Logger.Log(
                MessageLevel.Info,
                NuGetResources.Log_NoUpdatesAvailableForProject,
                package.Id,
                projectManager.ProjectManager.Project.ProjectName);
            return ops;
        }


        // Updates the specified package in projects
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "NuGet.ILogger.Log(NuGet.MessageLevel,System.String,System.Object[])")]
        private IEnumerable<Operation> ResolveOperationsToUpdateOnePackage(string id, SemanticVersion version, IEnumerable<IProjectManager> projectManagers, 
            bool projectNameSpecified)
        {
            IList<Operation> operations;

            var oldPackage = projectNameSpecified ?
                FindPackageToUpdate(id, version, Resolver.PackageManager, projectManagers.First()) :
                FindPackageToUpdate(id, version, Resolver.PackageManager, projectManagers, Logger);
            if (oldPackage.Item2 == null)
            {
                // we're updating a solution level package
                var package = Resolver.PackageManager.SourceRepository.FindPackage(
                    id, version, AllowPrereleaseVersions, allowUnlisted: false);
                if (package == null)
                {
                    Logger.Log(MessageLevel.Info, VsResources.NoUpdatesAvailable, id);
                    return new List<Operation>();
                }

                operations = Resolver.ResolveProjectOperations(
                    UserOperation.Install, 
                    package, 
                    projectManager: null);
            }
            else
            {
                var projectOperations = GetProjectOperationsForUpdate(
                    id,
                    version,
                    projectManagers.Select(p => new VirtualProjectManager(p)));
                operations = Resolver.ResolveFinalOperations(projectOperations);
            }

            return operations;
        }

        // Find the package that is to be updated when user specifies the project
        public static Tuple<IPackage, IProjectManager> FindPackageToUpdate(
            string id, SemanticVersion version, 
            IPackageManager packageManager,
            IProjectManager projectManager)
        {
            IPackage package = null;

            // Check if the package is installed in the project
            package = projectManager.LocalRepository.FindPackage(id, version: null);
            if (package != null)
            {
                return Tuple.Create(package, projectManager);
            }

            // The package could be a solution level pacakge.
            if (version != null)
            {
                package = packageManager.LocalRepository.FindPackage(id, version);
            }
            else
            {
                // Get all packages by this name to see if we find an ambiguous match
                var packages = packageManager.LocalRepository.FindPackagesById(id).ToList();
                if (packages.Count > 1)
                {
                    if (packages.Any(p => packageManager.IsProjectLevel(p)))
                    {
                        throw new InvalidOperationException(
                            String.Format(CultureInfo.CurrentCulture,
                                VsResources.UnknownPackageInProject,
                                packages[0].Id,
                                projectManager.Project.ProjectName));
                    }

                    throw new InvalidOperationException(
                            String.Format(CultureInfo.CurrentCulture,
                                VsResources.AmbiguousUpdate,
                                packages[0].Id));
                }

                // Pick the only one of default if none match
                package = packages.SingleOrDefault();
            }

            // Can't find the package in the solution or in the project then fail
            if (package == null)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.UnknownPackage, id));
            }

            bool isProjectLevel = packageManager.IsProjectLevel(package);
            if (isProjectLevel)
            {
                // The package is project level, but it is not referenced by the specified 
                // project. This is an error.
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

            return Tuple.Create<IPackage, IProjectManager>(package, null);
        }

        // Find the package that is to be updated.
        public static Tuple<IPackage, IProjectManager> FindPackageToUpdate(
            string id, SemanticVersion version, 
            IPackageManager packageManager,        
            IEnumerable<IProjectManager> projectManagers,
            ILogger logger)
        {
            IPackage package = null;

            // Check if the package is installed in a project
            foreach (var projectManager in projectManagers)
            {
                package = projectManager.LocalRepository.FindPackage(id, version: null);
                if (package != null)
                {
                    return Tuple.Create(package, projectManager);
                }
            }

            // Check if the package is a solution level package
            if (version != null)
            {
                package = packageManager.LocalRepository.FindPackage(id, version);
            }
            else
            {
                // Get all packages by this name to see if we find an ambiguous match
                var packages = packageManager.LocalRepository.FindPackagesById(id).ToList();
                foreach (var p in packages)
                {
                    bool isProjectLevel = packageManager.IsProjectLevel(p);

                    if (!isProjectLevel)
                    {
                        if (packages.Count > 1)
                        {
                            throw new InvalidOperationException(
                                String.Format(CultureInfo.CurrentCulture,
                                    VsResources.AmbiguousUpdate,
                                    id));
                        }

                        package = p;
                        break;
                    }
                    else
                    {
                        if (!packageManager.LocalRepository.IsReferenced(p.Id, p.Version))
                        {
                            logger.Log(MessageLevel.Warning, String.Format(CultureInfo.CurrentCulture,
                                VsResources.Warning_PackageNotReferencedByAnyProject, p.Id, p.Version));

                            // Try next package
                            continue;
                        }

                        // Found a package with package Id as 'id' which is installed in at least 1 project
                        package = p;
                        break;
                    }
                }

                if (package == null)
                {
                    // There are one or more packages with package Id as 'id'
                    // BUT, none of them is installed in a project
                    // it's probably a borked install.
                    throw new PackageNotInstalledException(
                        String.Format(CultureInfo.CurrentCulture,
                        VsResources.PackageNotInstalledInAnyProject, id));
                }
            }

            if (package == null)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.UnknownPackage, id));
            }

            return Tuple.Create<IPackage, IProjectManager>(package, null);
        }
    }
}
