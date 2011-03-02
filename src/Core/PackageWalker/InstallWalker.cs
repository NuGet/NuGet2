using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using NuGet.Resources;

namespace NuGet {
    public class InstallWalker : PackageWalker, IPackageOperationResolver {
        private readonly bool _ignoreDependencies;
        private readonly OperationLookup _operations;

        public InstallWalker(IPackageRepository localRepository,
                             IPackageRepository sourceRepository,
                             ILogger logger,
                             bool ignoreDependencies) {

            if (sourceRepository == null) {
                throw new ArgumentNullException("sourceRepository");
            }
            if (localRepository == null) {
                throw new ArgumentNullException("localRepository");
            }
            if (logger == null) {
                throw new ArgumentNullException("logger");
            }

            Repository = localRepository;
            Logger = logger;
            SourceRepository = sourceRepository;
            _ignoreDependencies = ignoreDependencies;
            _operations = new OperationLookup();
        }

        protected ILogger Logger {
            get;
            private set;
        }

        protected IPackageRepository Repository {
            get;
            private set;
        }

        protected override bool IgnoreDependencies {
            get {
                return _ignoreDependencies;
            }
        }

        protected IPackageRepository SourceRepository {
            get;
            private set;
        }

        protected IList<PackageOperation> Operations {
            get {
                return _operations.ToList();
            }
        }

        protected virtual ConflictResult GetConflict(string packageId) {
            var conflictingPackage = Marker.FindPackage(packageId);
            if (conflictingPackage != null) {
                return new ConflictResult(conflictingPackage, Marker, Marker);
            }
            return null;
        }

        protected override void OnBeforePackageWalk(IPackage package) {
            ConflictResult conflictResult = GetConflict(package.Id);

            if (conflictResult == null) {
                return;
            }

            // If the conflicting package is the same as the package being installed
            // then no-op
            if (PackageEqualityComparer.IdAndVersion.Equals(package, conflictResult.Package)) {
                return;
            }

            // First we get a list of dependents for the installed package.
            // Then we find the dependency in the foreach dependent that this installed package used to satisfy.
            // We then check if the resolved package also meets that dependency and if it doesn't it's added to the list
            // i.e A1 -> C >= 1
            //     B1 -> C >= 1
            //     C2 -> []
            // Given the above graph, if we upgrade from C1 to C2, we need to see if A and B can work with the new C
            var dependents = from dependentPackage in GetDependents(conflictResult)
                             where !IsDependencySatisfied(dependentPackage, package)
                             select dependentPackage;

            if (dependents.Any()) {
                throw CreatePackageConflictException(package, conflictResult.Package, dependents);
            }
            else if (package.Version < conflictResult.Package.Version) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.NewerVersionAlreadyReferenced, package.Id));
            }
            else if (package.Version > conflictResult.Package.Version) {
                // If this package isn't part of the current graph (i.e. hasn't been visited yet) and
                // is marked for removal, then do nothing. This is so we don't get unnecessary duplicates.
                if (!Marker.Contains(conflictResult.Package) &&
                    _operations.Contains(conflictResult.Package, PackageAction.Uninstall)) {
                    return;
                }

                // Uninstall the conflicting package. We set throw on conflicts to false since we've
                // already decided that there were no conflicts based on the above code.
                var resolver = new UninstallWalker(conflictResult.Repository,
                                                   conflictResult.DependentsResolver,
                                                   NullLogger.Instance,
                                                   removeDependencies: !IgnoreDependencies,
                                                   forceRemove: false) { ThrowOnConflicts = false };

                foreach (var operation in resolver.ResolveOperations(conflictResult.Package)) {
                    _operations.AddOperation(operation);
                }
            }
        }

        protected override void OnAfterPackageWalk(IPackage package) {
            if (!Repository.Exists(package)) {
                // Don't add the package for installation if it already exists in the repository
                _operations.AddOperation(new PackageOperation(package, PackageAction.Install));
            }
            else {
                // If we already added an entry for removing this package then remove it 
                // (it's equivalent for doing +P since we're removing a -P from the list)
                _operations.RemoveOperation(package, PackageAction.Uninstall);
            }
        }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            // See if we have a local copy
            IPackage package = Repository.FindDependency(dependency);

            if (package != null) {
                // We have it installed locally
                Logger.Log(MessageLevel.Debug, NuGetResources.Debug_DependencyAlreadyInstalled, dependency);
            }
            else {
                // We didn't resolve the dependency so try to retrieve it from the source
                Logger.Log(MessageLevel.Info, NuGetResources.Log_AttemptingToRetrievePackageFromSource, dependency);

                package = SourceRepository.FindDependency(dependency);

                if (package != null) {
                    Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageRetrieveSuccessfully);
                }
            }

            return package;
        }

        protected override void OnDependencyResolveError(PackageDependency dependency) {
            throw new InvalidOperationException(
                String.Format(CultureInfo.CurrentCulture,
                NuGetResources.UnableToResolveDependency, dependency));
        }

        public IEnumerable<PackageOperation> ResolveOperations(IPackage package) {
            _operations.Clear();
            Marker.Clear();

            Walk(package);
            return Operations.Reduce();
        }


        private IEnumerable<IPackage> GetDependents(ConflictResult conflict) {
            // Skip all dependents that are marked for uninstall
            IEnumerable<IPackage> packages = _operations.GetPackages(PackageAction.Uninstall);

            return conflict.DependentsResolver.GetDependents(conflict.Package)
                                              .Except(packages, PackageEqualityComparer.IdAndVersion);
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

            if (dependency.VersionSpec == null) {
                return true;
            }

            // Get the delegate for this version info and see if it returns true
            Func<IPackage, bool> versionMatcher = dependency.VersionSpec.ToDelegate();

            return versionMatcher(targetPackage);
        }

        /// <summary>
        /// Operation lookup encapsulates an operation list and another efficient data structure for finding package operations
        /// by package id, version and PackageAction.
        /// </summary>
        private class OperationLookup {
            private readonly List<PackageOperation> _operations = new List<PackageOperation>();
            private readonly Dictionary<PackageAction, Dictionary<IPackage, PackageOperation>> _operationLookup = new Dictionary<PackageAction, Dictionary<IPackage, PackageOperation>>();

            internal void Clear() {
                _operations.Clear();
                _operationLookup.Clear();
            }

            internal IList<PackageOperation> ToList() {
                return _operations;
            }

            internal IEnumerable<IPackage> GetPackages(PackageAction action) {
                Dictionary<IPackage, PackageOperation> dictionary = GetPackageLookup(action);
                if (dictionary != null) {
                    return dictionary.Keys;
                }
                return Enumerable.Empty<IPackage>();
            }

            internal void AddOperation(PackageOperation operation) {
                _operations.Add(operation);
                Dictionary<IPackage, PackageOperation> dictionary = GetPackageLookup(operation.Action, createIfNotExists: true);
                dictionary.Add(operation.Package, operation);
            }

            internal void RemoveOperation(IPackage package, PackageAction action) {
                Dictionary<IPackage, PackageOperation> dictionary = GetPackageLookup(action);
                PackageOperation operation;
                if (dictionary != null && dictionary.TryGetValue(package, out operation)) {
                    dictionary.Remove(package);
                    _operations.Remove(operation);
                }
            }

            internal bool Contains(IPackage package, PackageAction action) {
                Dictionary<IPackage, PackageOperation> dictionary = GetPackageLookup(action);
                return dictionary != null && dictionary.ContainsKey(package);
            }

            private Dictionary<IPackage, PackageOperation> GetPackageLookup(PackageAction action, bool createIfNotExists = false) {
                Dictionary<IPackage, PackageOperation> packages;
                if (!_operationLookup.TryGetValue(action, out packages) && createIfNotExists) {
                    packages = new Dictionary<IPackage, PackageOperation>(PackageEqualityComparer.IdAndVersion);
                    _operationLookup.Add(action, packages);
                }
                return packages;
            }
        }
    }
}
