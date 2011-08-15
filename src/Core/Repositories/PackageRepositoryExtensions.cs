using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NuGet {
    public static class PackageRepositoryExtensions {
        public static bool Exists(this IPackageRepository repository, IPackageMetadata package) {
            return repository.Exists(package.Id, package.Version);
        }

        public static bool Exists(this IPackageRepository repository, string packageId) {
            return Exists(repository, packageId, version: null);
        }

        public static bool Exists(this IPackageRepository repository, string packageId, Version version) {
            return repository.FindPackage(packageId, version) != null;
        }

        public static bool TryFindPackage(this IPackageRepository repository, string packageId, Version version, out IPackage package) {
            package = repository.FindPackage(packageId, version);
            return package != null;
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId) {
            return repository.FindPackage(packageId, version: null);
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, string versionSpec) {
            if (versionSpec == null) {
                throw new ArgumentNullException("versionSpec");
            }
            return repository.FindPackage(packageId, VersionUtility.ParseVersionSpec(versionSpec));
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, Version version) {
            return FindPackage(repository, packageId, version, constraintProvider: null);
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, Version version, IPackageConstraintProvider constraintProvider) {
            if (repository == null) {
                throw new ArgumentNullException("repository");
            }

            if (packageId == null) {
                throw new ArgumentNullException("packageId");
            }

            // If the repository implements it's own lookup then use that instead.
            // This is an optimization that we use so we don't have to enumerate packages for
            // sources that don't need to.
            var packageLookup = repository as IPackageLookup;
            if (packageLookup != null && version != null) {
                return packageLookup.FindPackage(packageId, version);
            }

            IEnumerable<IPackage> packages = repository.FindPackagesById(packageId)
                                                       .ToList()
                                                       .OrderByDescending(p => p.Version);

            if (version != null) {
                packages = packages.Where(p => VersionUtility.NormalizeVersion(p.Version) == 
                                               VersionUtility.NormalizeVersion(version));
            }
            else if (constraintProvider != null) {
                packages = FilterPackagesByConstraints(constraintProvider, packages, packageId);
            }

            return packages.FirstOrDefault();
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, IVersionSpec versionSpec, IPackageConstraintProvider constraintProvider) {
            var packages = repository.FindPackages(packageId, versionSpec);

            if (constraintProvider != null) {
                packages = FilterPackagesByConstraints(constraintProvider, packages, packageId);
            }

            return packages.FirstOrDefault();
        }

        public static IEnumerable<IPackage> FindPackages(this IPackageRepository repository, IEnumerable<string> packageIds) {
            if (packageIds == null) {
                throw new ArgumentNullException("packageIds");
            }

            return FindPackages(repository, packageIds, GetFilterExpression);
        }

        public static IEnumerable<IPackage> FindPackagesById(this IPackageRepository repository, string packageId) {
            return (from p in repository.GetPackages()
                    where p.Id.ToLower() == packageId.ToLower()
                    orderby p.Id
                    select p).ToList();
        }

        /// <summary>
        /// Since Odata dies when our query for updates is too big. We query for updates 10 packages at a time
        /// and return the full list of packages.
        /// </summary>
        private static IEnumerable<IPackage> FindPackages<T>(this IPackageRepository repository, IEnumerable<T> items, Func<IEnumerable<T>, Expression<Func<IPackage, bool>>> filterSelector) {
            const int batchSize = 10;

            while (items.Any()) {
                IEnumerable<T> currentItems = items.Take(batchSize);
                Expression<Func<IPackage, bool>> filterExpression = filterSelector(currentItems);

                var query = repository.GetPackages().Where(filterExpression).OrderBy(p => p.Id);
                foreach (var package in query) {
                    yield return package;
                }

                items = items.Skip(batchSize);
            }
        }

        public static IEnumerable<IPackage> FindPackages(this IPackageRepository repository, string packageId, IVersionSpec versionSpec) {
            if (repository == null) {
                throw new ArgumentNullException("repository");
            }

            if (packageId == null) {
                throw new ArgumentNullException("packageId");
            }

            IEnumerable<IPackage> packages = repository.FindPackagesById(packageId)
                                                       .OrderByDescending(p => p.Version);

            if (versionSpec != null) {
                packages = packages.FindByVersion(versionSpec);
            }

            return packages;
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, IVersionSpec versionSpec) {
            return repository.FindPackages(packageId, versionSpec).FirstOrDefault();
        }

        public static IEnumerable<IPackage> FindCompatiblePackages(this IPackageRepository repository, IPackageConstraintProvider constraintProvider, IEnumerable<string> packageIds, IPackage package) {
            return (from p in repository.FindPackages(packageIds)
                    let dependency = p.FindDependency(package.Id)
                    let otherConstaint = constraintProvider.GetConstraint(p.Id)
                    where dependency != null &&
                          dependency.VersionSpec.Satisfies(package.Version) &&
                          (otherConstaint == null || otherConstaint.Satisfies(package.Version))
                    select p);
        }

        public static PackageDependency FindDependency(this IPackageMetadata package, string packageId) {
            return (from dependency in package.Dependencies
                    where dependency.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)
                    select dependency).FirstOrDefault();
        }


        public static IQueryable<IPackage> GetPackages(this IPackageRepository repository, IEnumerable<string> targetFrameworks) {
            return Search(repository, searchTerm: null, targetFrameworks: targetFrameworks);
        }

        public static IQueryable<IPackage> Search(this IPackageRepository repository, string searchTerm) {
            return Search(repository, searchTerm, targetFrameworks: Enumerable.Empty<string>());
        }

        public static IQueryable<IPackage> Search(this IPackageRepository repository, string searchTerm, IEnumerable<string> targetFrameworks) {
            if (targetFrameworks == null) {
                throw new ArgumentNullException("targetFrameworks");
            }

            var searchableRepository = repository as ISearchableRepository;
            if (searchableRepository != null) {
                return searchableRepository.Search(searchTerm, targetFrameworks);
            }

            // Ignore the target framework if the repository doesn't support searching
            return repository.GetPackages().Find(searchTerm);
        }

        /// <summary>
        /// Returns a set of dependencies ordered by ascending versions while accounting for repositories that implement an IDependencyProvider
        /// </summary>
        public static IEnumerable<IPackage> GetDependencies(this IPackageRepository repository, string packageId) {
            return repository.FindPackagesById(packageId);
        }

        public static IPackage ResolveDependency(this IPackageRepository repository, PackageDependency dependency) {
            return ResolveDependency(repository, dependency: dependency, constraintProvider: null);
        }

        public static IPackage ResolveDependency(this IPackageRepository repository, PackageDependency dependency, IPackageConstraintProvider constraintProvider) {
            IDependencyResolver dependencyResolver = repository as IDependencyResolver;
            if (dependencyResolver != null) {
                return dependencyResolver.ResolveDependency(dependency, constraintProvider);
            }
            return ResolveDependencyCore(repository, dependency, constraintProvider);
        }

        internal static IPackage ResolveDependencyCore(this IPackageRepository repository, PackageDependency dependency, IPackageConstraintProvider constraintProvider) {
            if (repository == null) {
                throw new ArgumentNullException("repository");
            }

            if (dependency == null) {
                throw new ArgumentNullException("dependency");
            }

            IEnumerable<IPackage> packages = repository.GetDependencies(dependency.Id).ToList();

            // Always filter by constraints when looking for dependencies
            packages = FilterPackagesByConstraints(constraintProvider, packages, dependency.Id);

            // If version info was specified then use it
            if (dependency.VersionSpec != null) {
                packages = packages.FindByVersion(dependency.VersionSpec);
            }
            else {
                // BUG 840: If no version info was specified then pick the latest
                return packages.OrderByDescending(p => p.Version)
                               .FirstOrDefault();
            }

            return packages.ResolveSafeVersion();
        }

        /// <summary>
        /// Returns updates for packages from the repository 
        /// </summary>
        /// <param name="repository">The repository to search for updates</param>
        /// <param name="packages">Packages to look for updates</param>
        /// <returns></returns>
        public static IEnumerable<IPackage> GetUpdates(this IPackageRepository repository, IEnumerable<IPackage> packages) {
            List<IPackage> packageList = packages.ToList();

            if (!packageList.Any()) {
                yield break;
            }

            // These are the packages that we need to look at for potential updates.
            IDictionary<string, IPackage> sourcePackages = GetUpdateCandidates(repository, packageList)
                                                                           .ToList()
                                                                           .GroupBy(package => package.Id)
                                                                           .ToDictionary(package => package.Key,
                                                                                         package => package.OrderByDescending(p => p.Version).First());

            foreach (IPackage package in packageList) {
                IPackage newestAvailablePackage;
                if (sourcePackages.TryGetValue(package.Id, out newestAvailablePackage) &&
                    VersionUtility.NormalizeVersion(newestAvailablePackage.Version) > 
                    VersionUtility.NormalizeVersion(package.Version)) {
                    yield return newestAvailablePackage;
                }
            }
        }
        
        /// <summary>
        /// Since odata dies when our query for updates is too big. We query for updates 10 packages at a time
        /// and return the full list of candidates for updates.
        /// </summary>
        private static IEnumerable<IPackage> GetUpdateCandidates(IPackageRepository repository, IEnumerable<IPackage> packages) {
            return FindPackages(repository, packages, GetFilterExpression);
        }

        /// <summary>
        /// For the list of input packages generate an expression like:
        /// p => p.Id == 'package1id' or p.Id == 'package2id' or p.Id == 'package3id'... up to package n
        /// </summary>
        private static Expression<Func<IPackage, bool>> GetFilterExpression(IEnumerable<IPackage> packages) {
            return GetFilterExpression(packages.Select(p => p.Id));
        }

        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower", Justification = "This is for a linq query")]
        private static Expression<Func<IPackage, bool>> GetFilterExpression(IEnumerable<string> ids) {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(IPackageMetadata));
            Expression expressionBody = ids.Select(id => GetCompareExpression(parameterExpression, id.ToLower()))
                                                .Aggregate(Expression.OrElse);

            return Expression.Lambda<Func<IPackage, bool>>(expressionBody, parameterExpression);
        }

        /// <summary>
        /// Builds the expression: package.Id.ToLower() == "somepackageid"
        /// </summary>
        private static Expression GetCompareExpression(Expression parameterExpression, object value) {
            // package.Id
            Expression propertyExpression = Expression.Property(parameterExpression, "Id");
            // .ToLower()
            Expression toLowerExpression = Expression.Call(propertyExpression, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            // == localPackage.Id
            return Expression.Equal(toLowerExpression, Expression.Constant(value));
        }

        private static IEnumerable<IPackage> FilterPackagesByConstraints(IPackageConstraintProvider constraintProvider, IEnumerable<IPackage> packages, string packageId) {
            if (constraintProvider == null) {
                return packages;
            }

            // Filter packages by this constraint
            IVersionSpec constraint = constraintProvider.GetConstraint(packageId);
            if (constraint != null) {
                return packages.FindByVersion(constraint);
            }

            return packages;
        }

        internal static IPackage ResolveSafeVersion(this IEnumerable<IPackage> packages) {
            // Return null if there's no packages
            if (packages == null || !packages.Any()) {
                return null;
            }

            // We want to take the biggest build and revision number for the smallest
            // major and minor combination (we want to make some versioning assumptions that the 3rd number is a non-breaking bug fix). This is so that we get the closest version
            // to the dependency, but also get bug fixes without requiring people to manually update the nuspec.
            // For example, if A -> B 1.0.0 and the feed has B 1.0.0 and B 1.0.1 then the more correct choice is B 1.0.1. 
            // If we don't do this, A will always end up getting the 'buggy' 1.0.0, 
            // unless someone explicitly changes it to ask for 1.0.1, which is very painful if many packages are using B 1.0.0.
            var groups = from p in packages
                         group p by new { p.Version.Major, p.Version.Minor } into g
                         orderby g.Key.Major, g.Key.Minor
                         select g;

            return (from p in groups.First()
                    orderby p.Version descending
                    select p).FirstOrDefault();
        }

        // HACK: We need this to avoid a partial trust issue. We need to be able to evaluate closures
        // within this class. The attributes are necessary to prevent this method from being inlined into ClosureEvaluator 
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static object Eval(FieldInfo fieldInfo, object obj) {
            return fieldInfo.GetValue(obj);
        }
    }
}
