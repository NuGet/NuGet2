using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuPack {
    public static class PackageRepositoryExtensions {
        private static readonly string[] _packagePropertiesToSearch = new[] { "Id", "Description" };

        internal static bool IsPackageInstalled(this IPackageRepository repository, IPackage package) {
            return repository.IsPackageInstalled(package.Id, package.Version);
        }

        internal static bool IsPackageInstalled(this IPackageRepository repository, string packageId) {
            return IsPackageInstalled(repository, packageId, version: null);
        }

        internal static bool IsPackageInstalled(this IPackageRepository repository, string packageId, Version version) {
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

        /// <summary>
        /// Looks up packages that contains one or more searchTerm in its metadata
        /// </summary>
        public static IQueryable<IPackage> GetPackages(this IPackageRepository repository, params string[] searchTerms) {
            var packages = repository.GetPackages();
            if (searchTerms == null || !searchTerms.Any()) {
                return packages;
            }
            return packages.Where(BuildSearchExpression(searchTerms));
        }


        public static IEnumerable<IPackage> GetUpdates(this IPackageRepository repository, IPackageRepository sourceRepository) {
            List<IPackage> packages = repository.GetPackages().ToList();

            if (!packages.Any()) {
                yield break;
            }

            // Filter packages by what we currently have installed
            ParameterExpression parameterExpression = Expression.Parameter(typeof(IPackage));
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

        /// <summary>
        /// Constructs an expression to search for individual tokens in a search term in the Id and Description of packages
        /// </summary>
        private static Expression<Func<IPackage, bool>> BuildSearchExpression(IEnumerable<string> searchTerms) {
            Debug.Assert(searchTerms != null);

            Expression condition = searchTerms.SelectMany(term => BuildExpressionsForTerm(term)).Aggregate(Expression.OrElse);
            return Expression.Lambda<Func<IPackage, bool>>(condition, Expression.Parameter(typeof(IPackage)));
        }

        private static IEnumerable<Expression> BuildExpressionsForTerm(string term) {
            var packageType = typeof(IPackage);
            var stringType = typeof(String);
            var packageTypeExpression = Expression.Parameter(packageType);
            MethodInfo stringContains = stringType.GetMethod("Contains");
            MethodInfo stringToLower = stringType.GetMethod("ToLower", Type.EmptyTypes);

            foreach (var propertyName in _packagePropertiesToSearch) {
                var propertyExpression = Expression.Property(packageTypeExpression, propertyName);
                var toLowerExpression = Expression.Call(propertyExpression, stringToLower);
                yield return Expression.Call(toLowerExpression, stringContains, Expression.Constant(term.ToLower()));
            }
        }

        private static IQueryable<IPackage> FindPackagesById(this IPackageRepository repository, string packageId) {
            return from p in repository.GetPackages()
                   where p.Id.ToLower() == packageId.ToLower()
                   select p;
        }
    }
}
