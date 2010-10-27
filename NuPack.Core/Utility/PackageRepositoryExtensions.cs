using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace NuGet {
    public static class PackageRepositoryExtensions {

        public static bool Exists(this IPackageRepository repository, IPackageMetadata package) {
            return repository.Exists(package.Id, package.Version);
        }

        public static bool Exists(this IPackageRepository repository, string packageId) {
            return Exists(repository, packageId, version: null);
        }

        public static bool Exists(this IPackageRepository repository, string packageId, Version version) {
            return repository.FindPackage(packageId, null, null, version) != null;
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId) {
            return FindPackage(repository, packageId, exactVersion: null, minVersion: null, maxVersion: null);
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, Version exactVersion) {
            return FindPackage(repository, packageId, exactVersion: exactVersion, minVersion: null, maxVersion: null);
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, Version minVersion, Version maxVersion) {
            return FindPackage(repository, packageId, minVersion: minVersion, maxVersion: maxVersion, exactVersion: null);
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, Version minVersion, Version maxVersion, Version exactVersion) {
            return repository.FindPackagesById(packageId).FindByVersion(minVersion, maxVersion, exactVersion);
        }

        public static IPackage FindPackage(this IPackageRepository repository, PackageDependency dependency) {
            return repository.FindPackage(dependency.Id, dependency.MinVersion, dependency.MaxVersion, dependency.Version);
        }

        /// <summary>
        /// Looks up packages that contains one or more searchTerm in its metadata
        /// </summary>
        public static IQueryable<IPackage> GetPackages(this IPackageRepository repository, params string[] searchTerms) {
            return repository.GetPackages().Find(searchTerms);
        }

        public static IEnumerable<IPackage> GetUpdates(this IPackageRepository repository, IPackageRepository sourceRepository, params string[] searchTerms) {
            List<IPackage> packages = repository.GetPackages(searchTerms).ToList();

            if (!packages.Any()) {
                yield break;
            }

            // Filter packages by what we currently have installed
            ParameterExpression parameterExpression = Expression.Parameter(typeof(IPackageMetadata));
            Expression expressionBody = packages.Select(package => GetCompareExpression(parameterExpression, package))
                                                .Aggregate(Expression.OrElse);

            var filterExpression = Expression.Lambda<Func<IPackage, bool>>(expressionBody, parameterExpression);

            // These are the packages that we need to look at for potential updates.
            IDictionary<string, IPackage> sourcePackages = sourceRepository.GetPackages()
                                                                           .Where(filterExpression)
                                                                           .AsEnumerable()
                                                                           .GroupBy(package => package.Id)
                                                                           .ToDictionary(package => package.Key,
                                                                                         package => package.OrderByDescending(p => p.Version).First());

            foreach (IPackage package in packages) {
                IPackage newestAvailablePackage;
                if (sourcePackages.TryGetValue(package.Id, out newestAvailablePackage) &&
                    newestAvailablePackage.Version > package.Version) {
                    yield return newestAvailablePackage;
                }
            }
        }

        /// <summary>
        /// Builds the expression: package.Id.ToLower() == "somepackageid"
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower", Justification = "This is for a linq query")]
        private static Expression GetCompareExpression(Expression parameterExpression, IPackage package) {
            // package.Id
            Expression propertyExpression = Expression.Property(parameterExpression, "Id");
            // .ToLower()
            Expression toLowerExpression = Expression.Call(propertyExpression, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            // == localPackage.Id
            return Expression.Equal(toLowerExpression, Expression.Constant(package.Id.ToLower()));
        }

        private static IQueryable<IPackage> FindPackagesById(this IPackageRepository repository, string packageId) {
            return from p in repository.GetPackages()
                   where p.Id.ToLower() == packageId.ToLower()
                   select p;
        }
    }
}
