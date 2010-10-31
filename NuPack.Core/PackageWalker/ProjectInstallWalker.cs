namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using NuGet.Resources;

    public class ProjectInstallWalker : InstallWalker {
        public ProjectInstallWalker(IPackageRepository localRepository,
                                    IPackageRepository sourceRepository,
                                    IDependentsResolver dependentsResolver,
                                    ILogger logger,
                                    bool ignoreDependencies)
            : base(localRepository, sourceRepository, logger, ignoreDependencies) {
            if (dependentsResolver == null) {
                throw new ArgumentNullException("dependentsResolver");
            }
            DependentsResolver = dependentsResolver;
        }

        protected IDependentsResolver DependentsResolver {
            get;
            private set;
        }

        protected override void OnBeforeDependencyWalk(IPackage package) {
            IPackage installedPackage = Repository.FindPackage(package.Id);

            // Package isn't installed so do nothing
            if (installedPackage == null) {
                return;
            }

            // First we get a list of dependents for the installed package.
            // Then we find the dependency in the foreach dependent that this installed package used to satisfy.
            // We then check if the resolved package also meets that dependency and if it doesn't it's added to the list
            // i.e A1 -> C >= 1
            //     B1 -> C >= 1
            //     C2 -> []
            // Given the above graph, if we upgrade from C1 to C2, we need to see if A and B can work with the new C
            var dependents = from dependentPackage in GetDependents(installedPackage)
                             where !IsDependencySatisfied(dependentPackage, package)
                             select dependentPackage;

            if (dependents.Any()) {
                throw CreatePackageConflictException(package, installedPackage, dependents);
            }
            else if (package.Version < installedPackage.Version) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.NewerVersionAlreadyReferenced, package.Id));
            }
            else if (package.Version > installedPackage.Version) {
                // Turn warnings off, since were forcing uninstall
                IPackageOperationResolver resolver = new UninstallWalker(Repository, DependentsResolver, NullLogger.Instance, !IgnoreDependencies, forceRemove: true);

                foreach (var operation in resolver.ResolveOperations(installedPackage)) {
                    Operations.Add(operation);
                }
            }
        }

        protected override void OnAfterDependencyWalk(IPackage package) {
            PackageWalkInfo info = GetPackageInfo(package);

            if (info.Target == PackageTargets.Project) {
                base.OnAfterDependencyWalk(package);
            }
        }

        private IEnumerable<IPackage> GetDependents(IPackage package) {
            // Skip all dependents that are marked for uninstall
            IEnumerable<IPackage> packages = from o in Operations
                                             where o.Action == PackageAction.Uninstall
                                             select o.Package;

            return DependentsResolver.GetDependents(package)
                                     .Except(packages, PackageComparer.IdAndVersionComparer);
        }

        private static InvalidOperationException CreatePackageConflictException(IPackage resolvedPackage, IPackage package, IEnumerable<IPackage> dependents) {
            if (dependents.Count() == 1) {
                return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                       NuGetResources.ConflictErrorWithDependent, package.GetFullName(), resolvedPackage.GetFullName(), dependents.Single().GetFullName()));
            }

            return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.ConflictErrorWithDependents, package.GetFullName(), resolvedPackage.GetFullName(), String.Join(", ",
                        dependents.Select(d => d.GetFullName()))));

        }

        private static bool IsDependencySatisfied(IPackage package, IPackage targetPackage) {
            PackageDependency dependency = (from d in package.Dependencies
                                            where d.Id.Equals(targetPackage.Id, StringComparison.OrdinalIgnoreCase)
                                            select d).FirstOrDefault();

            Debug.Assert(dependency != null, "Package doesn't have this dependency");

            // Given a package's dependencies and a target package we want to see if the target package
            // satisfies the package's dependencies i.e:
            // A 1.0 -> B 1.0
            // A 2.0 -> B 2.0
            // C 1.0 -> B (>= 1.0) (min version 1.0)
            // Updating to A 2.0 from A 1.0 needs to know if there is a conflict with C
            // Since C works with B (>= 1.0) it it should be ok to update A

            // If there is an exact version specified then we check if the package is that exact version
            if (dependency.Version != null) {
                return dependency.Version.Equals(targetPackage.Version);
            }

            bool isSatisfied = true;

            // See if it meets the minimum version requirement if any
            if (dependency.MinVersion != null) {
                isSatisfied = targetPackage.Version >= dependency.MinVersion;
            }

            // See if it meets the maximum version requirement if any
            if (dependency.MaxVersion != null) {
                isSatisfied = isSatisfied && targetPackage.Version <= dependency.MaxVersion;
            }

            return isSatisfied;
        }
    }
}
