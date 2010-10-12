namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NuPack.Resources;

    public class ProjectInstallWalker : InstallWalker {
        public ProjectInstallWalker(IPackageRepository localRepository,
                                    IPackageRepository sourceRepository,
                                    IDependencyResolver dependentsResolver,
                                    ILogger logger,
                                    bool ignoreDependencies)
            : base(localRepository, sourceRepository, logger, ignoreDependencies) {
            if (dependentsResolver == null) {
                throw new ArgumentNullException("dependentsResolver");
            }
            DependentsResolver = dependentsResolver;
        }

        protected IDependencyResolver DependentsResolver {
            get;
            private set;
        }

        protected virtual IEnumerable<IPackage> GetDependents(IPackage package) {
            // Get all dependents for this package that we don't want to skip
            return DependentsResolver.ResolveDependencies(package);
        }

        protected override void LogDependencyExists(PackageDependency dependency) {
            Logger.Log(MessageLevel.Debug, NuPackResources.Debug_DependencyAlreadyReferenced, dependency);
        }

        protected override void LogRetrieveDependencyFromSource(PackageDependency dependency) {
            Logger.Log(MessageLevel.Info, NuPackResources.Log_AttemptingToRetrievePackageReferenceFromSource, dependency);
        }

        protected override void ProcessResolvedDependency(IPackage package, PackageDependency dependency, IPackage resolvedDependency) {
            IPackage installedPackage = Repository.FindPackage(dependency.Id);

            if (installedPackage != null) {
                CheckConflict(resolvedDependency, installedPackage);
            }
        }

        protected void CheckConflict(IPackage resolvedDependency, IPackage installedPackage) {
            // First we get a list of dependents for the installed package.
            // Then we find the dependency in the foreach dependent that this installed package used to satisfy.
            // We then check if the resolved package also meets that dependency and if it doesn't it's added to the list
            // i.e A1 -> C >= 1
            //     B1 -> C >= 1
            //     C2 -> []
            // Given the above graph, if we upgrade from C1 to C2, we need to see if A and B can work with the new C
            var dependents = from dependentPackage in GetDependents(installedPackage)
                             where !dependentPackage.IsDependencySatisfied(resolvedDependency)
                             select dependentPackage;

            if (dependents.Any()) {
                throw CreatePackageConflictException(resolvedDependency, installedPackage, dependents);
            }
        }

        private static InvalidOperationException CreatePackageConflictException(IPackage resolvedPackage, IPackage package, IEnumerable<IPackage> dependents) {
            if (dependents.Count() == 1) {
                return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                       NuPackResources.ConflictErrorWithDependent, package.GetFullName(), resolvedPackage.GetFullName(), dependents.Single().GetFullName()));
            }

            return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                        NuPackResources.ConflictErrorWithDependents, package.GetFullName(), resolvedPackage.GetFullName(), String.Join(", ",
                        dependents.Select(d => d.GetFullName()))));

        }
    }
}