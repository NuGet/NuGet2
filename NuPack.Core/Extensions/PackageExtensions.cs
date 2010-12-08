using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuGet {
    public static class PackageExtensions {
        private static readonly string[] _packagePropertiesToSearch = new[] { "Id", "Description" };

        public static IEnumerable<IPackage> FindByVersion(this IEnumerable<IPackage> source, IVersionSpec versionSpec) {
            if (versionSpec == null) {
                throw new ArgumentNullException("versionSpec");
            }

            return source.Where(versionSpec.ToDelegate());
        }

        public static IEnumerable<IPackageFile> GetFiles(this IPackage package, string directory) {
            return package.GetFiles().Where(file => file.Path.StartsWith(directory, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<IPackageFile> GetContentFiles(this IPackage package) {
            return package.GetFiles(Constants.ContentDirectory);
        }

        /// <summary>
        /// Returns true if a package has no content that applies to a project.
        /// </summary>
        public static bool HasProjectContent(this IPackage package) {
            return package.AssemblyReferences.Any() || package.GetContentFiles().Any();
        }

        /// <summary>
        /// Returns true if a package has dependencies but no files.
        /// </summary>
        public static bool IsDependencyOnly(this IPackage package) {
            return !package.GetFiles().Any() && package.Dependencies.Any();
        }

        public static string GetFullName(this IPackageMetadata package) {
            return package.Id + " " + package.Version;
        }

        /// <summary>
        /// Reduces the number of operations to the minimal set
        /// </summary>
        internal static IEnumerable<PackageOperation> Reduce(this IEnumerable<PackageOperation> operations) {
            // Break the set into 2 lists and remove duplicates
            var removeList = operations.Where(o => o.Action == PackageAction.Uninstall)
                                       .Distinct()
                                       .Select(o => o.Package);

            var addList = operations.Where(o => o.Action == PackageAction.Install)
                                    .Distinct()
                                    .Select(o => o.Package);

            // Find the reduced set of packages to install and uninstall by taking the set difference in both ways
            // i.e. if there are 2 lists:
            // removedList = [-A, -B, -D]
            // addList= [+A, +B, +C]
            // reducedRemovedList = [-D]
            // reducedAddedList = [+C]
            var reducedRemoveList = from p in removeList.Except(addList, PackageEqualityComparer.IdAndVersion)
                                    select new PackageOperation(p, PackageAction.Uninstall);

            var reducedAddList = from p in addList.Except(removeList, PackageEqualityComparer.IdAndVersion)
                                 select new PackageOperation(p, PackageAction.Install);
            
            return reducedRemoveList.Union(reducedAddList);
        }

        public static IQueryable<IPackage> Find(this IQueryable<IPackage> packages, params string[] searchTerms) {
            if (searchTerms == null) {
                return packages;
            }

            IEnumerable<string> nonNullTerms = searchTerms.Where(s => s != null);
            if (!nonNullTerms.Any()) {
                return packages;
            }

            return packages.Where(BuildSearchExpression(nonNullTerms));
        }

        /// <summary>
        /// Constructs an expression to search for individual tokens in a search term in the Id and Description of packages
        /// </summary>
        private static Expression<Func<IPackage, bool>> BuildSearchExpression(IEnumerable<string> searchTerms) {
            Debug.Assert(searchTerms != null);
            var parameterExpression = Expression.Parameter(typeof(IPackageMetadata));
            // package.Id.ToLower().Contains(term1) || package.Id.ToLower().Contains(term2)  ...
            Expression condition = (from term in searchTerms
                                    from property in _packagePropertiesToSearch
                                    select BuildExpressionForTerm(parameterExpression, term, property)).Aggregate(Expression.OrElse);
            return Expression.Lambda<Func<IPackage, bool>>(condition, parameterExpression);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower",
            Justification = "The expression is remoted using Odata which does not support the culture parameter")]
        private static Expression BuildExpressionForTerm(ParameterExpression packageParameterExpression, string term, string propertyName) {
            MethodInfo stringContains = typeof(String).GetMethod("Contains");
            MethodInfo stringToLower = typeof(String).GetMethod("ToLower", Type.EmptyTypes);

            // package.Id / package.Description
            var propertyExpression = Expression.Property(packageParameterExpression, propertyName);
            // .ToLower()
            var toLowerExpression = Expression.Call(propertyExpression, stringToLower);
            // .Contains(term.ToLower())
            return Expression.Call(toLowerExpression, stringContains, Expression.Constant(term.ToLower()));
        }
    }
}
