using System;
using System.Collections.Generic;

namespace NuGet.Resolver
{
    public enum UserOperation
    {
        Install,
        Uninstall
    }

    public class OperationResolver
    {
        public IPackageManager PackageManager { get; private set; }

        public DependencyVersion DependencyVersion { get; set; }

        // used by install & update 
        public bool IgnoreDependencies { get; set; }

        public bool AllowPrereleaseVersions { get; set; }

        public bool ForceRemove { get; set; }

        // used by uninstall
        public bool RemoveDependencies { get; set; }

        public ILogger Logger { get; set; }

        public OperationResolver(IPackageManager packageManager)
        {
            
            PackageManager = packageManager;
            Logger = NullLogger.Instance;
            DependencyVersion = DependencyVersion.Lowest;
        }

        public IList<Operation> ResolveProjectOperations(
            UserOperation userOperation,
            IPackage package,
            VirtualProjectManager projectManager)
        {
            switch (userOperation)
            {
                case UserOperation.Install:
                    return ResolveProjectOperationsForInstall(package, projectManager);
                case UserOperation.Uninstall:
                    return ResolveProjectOperationsForUninstall(package, projectManager);
                default:
                    throw new ArgumentException("Invalid value", "userOperation");
            }
        }

        // Resolve operations to install a package into projects
        // If projectManager is null, the package is a solution level package.
        private IList<Operation> ResolveProjectOperationsForInstall(
            IPackage package,
            VirtualProjectManager projectManager)
        {
            IEnumerable<PackageOperation> projectOperations;
            if (projectManager == null)
            {
                // install a solution level package
                var updateWalker = new UpdateWalker(
                    PackageManager.LocalRepository,
                    PackageManager.SourceRepository,
                    new DependentsWalker(PackageManager.LocalRepository, targetFramework: null),
                    NullConstraintProvider.Instance,
                    targetFramework: null,
                    logger: Logger,
                    updateDependencies: !IgnoreDependencies,
                    allowPrereleaseVersions: AllowPrereleaseVersions);

                projectOperations = updateWalker.ResolveOperations(package);

                // we're uninstalling solution level packages. All target should be
                // set to PackagesFolder.
                foreach (var op in projectOperations)
                {
                    op.Target = PackageOperationTarget.PackagesFolder;
                }
            }
            else
            {
                // resolve project operations
                var dependentsWalker = new DependentsWalker(
                    PackageManager.LocalRepository,
                    projectManager.ProjectManager.GetTargetFrameworkForPackage(package.Id))
                {
                    DependencyVersion = DependencyVersion
                };
                var updateWalker = new UpdateWalker(
                    projectManager.LocalRepository,
                    PackageManager.SourceRepository,
                    dependentsWalker,
                    projectManager.ProjectManager.ConstraintProvider,
                    projectManager.ProjectManager.Project.TargetFramework,
                    Logger ?? NullLogger.Instance,
                    !IgnoreDependencies,
                    AllowPrereleaseVersions)
                {
                    AcceptedTargets = PackageTargets.All,
                    DependencyVersion = DependencyVersion
                };

                projectOperations = updateWalker.ResolveOperations(package);
            }

            var realProjectOperations = new List<Operation>();
            foreach (var operation in projectOperations)
            {
                if (operation.Target == PackageOperationTarget.Project)
                {
                    realProjectOperations.Add(new Operation(
                        operation,
                        projectManager: projectManager.ProjectManager,
                        packageManager: null));

                    if (operation.Action == PackageAction.Install)
                    {
                        projectManager.LocalRepository.AddPackage(operation.Package);
                    }
                    else
                    {
                        projectManager.LocalRepository.RemovePackage(operation.Package);
                    }
                }
                else
                {
                    realProjectOperations.Add(new Operation(
                        operation,
                        projectManager: null,
                        packageManager: PackageManager));
                }
            }

            return realProjectOperations;
        }

        public IList<Operation> ResolveFinalOperations(IEnumerable<Operation> projectOperations)
        {
            var packageRefCount = new Dictionary<IPackage, int>(PackageEqualityComparer.IdAndVersion);
            foreach (var repo in PackageManager.LocalRepository.LoadProjectRepositories())
            {
                foreach (var p in repo.GetPackages())
                {
                    if (packageRefCount.ContainsKey(p))
                    {
                        packageRefCount[p]++;
                    }
                    else
                    {
                        packageRefCount[p] = 1;
                    }
                }
            }

            // generate operations
            var packagesFolderInstallOperations = new List<Operation>();
            var packagesFolderUninstallOperations = new List<Operation>();

            foreach (var operation in projectOperations)
            {
                if (operation.Action == PackageAction.Uninstall)
                {
                    // update the package's ref count
                    if (packageRefCount.ContainsKey(operation.Package))
                    {
                        packageRefCount[operation.Package]--;
                        if (packageRefCount[operation.Package] <= 0)
                        {
                            packagesFolderUninstallOperations.Add(
                                new Operation(
                                    new PackageOperation(operation.Package, PackageAction.Uninstall)
                                    {
                                        Target = PackageOperationTarget.PackagesFolder
                                    },
                                projectManager: null,
                                packageManager: PackageManager));
                        }
                    }
                }
                else
                {
                    int refCount;
                    if (packageRefCount.TryGetValue(operation.Package, out refCount) &&
                        refCount > 0)
                    {
                        // package already exists in packages folder
                    }
                    else
                    {
                        packageRefCount.Add(operation.Package, 1);

                        // package does not exist in packages folder. We need to add
                        // an install into packages folder operation if op.Target is project
                        if (operation.Target == PackageOperationTarget.Project)
                        {
                            packagesFolderInstallOperations.Add(
                                new Operation(
                                    new PackageOperation(operation.Package, PackageAction.Install)
                                    {
                                        Target = PackageOperationTarget.PackagesFolder
                                    },
                                projectManager: null,
                                packageManager: PackageManager));
                        }
                    }
                }
            }

            var operations = new List<Operation>();
            operations.AddRange(packagesFolderInstallOperations);
            operations.AddRange(projectOperations);
            operations.AddRange(packagesFolderUninstallOperations);

            return operations;
        }

        // Resolve operations to uninstall a package.
        // If projectManager is null, the package is a solution level package.
        private IList<Operation> ResolveProjectOperationsForUninstall(
            IPackage package,
            VirtualProjectManager projectManager)
        {
            IEnumerable<PackageOperation> projectOperations = null;
            if (projectManager != null)
            {
                var targetFramework = projectManager.ProjectManager.GetTargetFrameworkForPackage(package.Id);
                var resolver = new UninstallWalker(
                    projectManager.LocalRepository,
                    new DependentsWalker(projectManager.LocalRepository, targetFramework),
                    targetFramework,
                    NullLogger.Instance,
                    RemoveDependencies,
                    ForceRemove);
                projectOperations = resolver.ResolveOperations(package);
            }
            else
            {
                var resolver = new UninstallWalker(
                    PackageManager.LocalRepository,
                    new DependentsWalker(
                        PackageManager.LocalRepository,
                        targetFramework: null),
                    targetFramework: null,
                    logger: Logger,
                    removeDependencies: RemoveDependencies,
                    forceRemove: ForceRemove);
                projectOperations = resolver.ResolveOperations(package);

                // we're uninstalling solution level packages. All target should be
                // set to PackagesFolder.
                foreach (var op in projectOperations)
                {
                    op.Target = PackageOperationTarget.PackagesFolder;
                }
            }

            var realProjectOperations = new List<Operation>();
            foreach (var operation in projectOperations)
            {
                if (operation.Target == PackageOperationTarget.Project)
                {
                    realProjectOperations.Add(new Operation(
                        operation, 
                        projectManager: projectManager.ProjectManager,
                        packageManager: null));

                    if (operation.Action == PackageAction.Install)
                    {
                        projectManager.LocalRepository.AddPackage(operation.Package);
                    }
                    else
                    {
                        projectManager.LocalRepository.RemovePackage(operation.Package);
                    }
                }
                else
                {
                    realProjectOperations.Add(new Operation(
                        operation, 
                        projectManager: null, 
                        packageManager: PackageManager));
                }
            }

            return realProjectOperations;
        }
    }    
}
