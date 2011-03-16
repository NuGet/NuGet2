using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
                packages = packages.Where(p => p.Version == version);
            }

            return packages.FirstOrDefault();
        }

        public static IEnumerable<IPackage> FindPackages(this IPackageRepository repository, IEnumerable<string> packageIds) {
            if (packageIds == null) {
                throw new ArgumentNullException("packageIds");
            }

            return FindPackages(repository, packageIds, GetFilterExpression);
        }

        public static IQueryable<IPackage> FindPackagesById(this IPackageRepository repository, string packageId) {
            return from p in repository.GetPackages()
                   where p.Id.ToLower() == packageId.ToLower()
                   orderby p.Id
                   select p;
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

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, IVersionSpec versionInfo) {
            if (repository == null) {
                throw new ArgumentNullException("repository");
            }

            if (packageId == null) {
                throw new ArgumentNullException("packageId");
            }

            IEnumerable<IPackage> packages = repository.FindPackagesById(packageId)
                                                       .ToList()
                                                       .OrderByDescending(p => p.Version);

            if (versionInfo != null) {
                packages = packages.FindByVersion(versionInfo);
            }

            return packages.FirstOrDefault();
        }

        public static IPackage FindDependency(this IPackageRepository repository, PackageDependency dependency) {
            if (repository == null) {
                throw new ArgumentNullException("repository");
            }

            if (dependency == null) {
                throw new ArgumentNullException("dependency");
            }

            // When looking for dependencies, order by lowest version
            IEnumerable<IPackage> packages = repository.FindPackagesById(dependency.Id)
                                                       .ToList();

            // If version info was specified then use it
            if (dependency.VersionSpec != null) {
                packages = packages.FindByVersion(dependency.VersionSpec);
            }
            else {
                // BUG 840: If no version info was specified then pick the latest
                return packages.OrderByDescending(p => p.Version)
                               .FirstOrDefault();
            }

            if (packages.Any()) {
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

            return null;
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
                    newestAvailablePackage.Version > package.Version) {
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

        // HACK: We need this to avoid a partial trust issue. We need to be able to evaluate closures
        // within this class
        internal static object Eval(FieldInfo fieldInfo, object obj) {
            return fieldInfo.GetValue(obj);
        }
    }
}
