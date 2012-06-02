using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Versioning;

namespace NuGet
{
    public static class PackageExtensions
    {
        private const string TagsProperty = "Tags";
        private static readonly string[] _packagePropertiesToSearch = new[] { "Id", "Description", TagsProperty };

        public static bool IsReleaseVersion(this IPackageMetadata packageMetadata)
        {
            return String.IsNullOrEmpty(packageMetadata.Version.SpecialVersion);
        }

        public static bool IsListed(this IPackage package)
        {
            return package.Listed || package.Published > Constants.Unpublished;
        }

        public static bool IsEmptyFolder(this IPackageFile packageFile)
        {
            return packageFile != null && 
                   Constants.PackageEmptyFileName.Equals(Path.GetFileName(packageFile.Path), StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<IPackage> FindByVersion(this IEnumerable<IPackage> source, IVersionSpec versionSpec)
        {
            if (versionSpec == null)
            {
                throw new ArgumentNullException("versionSpec");
            }

            return source.Where(versionSpec.ToDelegate());
        }

        public static IEnumerable<IPackageFile> GetFiles(this IPackage package, string directory)
        {
            string folderPrefix = directory + Path.DirectorySeparatorChar;
            return package.GetFiles().Where(file => file.Path.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<IPackageFile> GetContentFiles(this IPackage package)
        {
            return package.GetFiles(Constants.ContentDirectory);
        }

        public static IEnumerable<IPackageFile> GetToolFiles(this IPackage package)
        {
            return package.GetFiles(Constants.ToolsDirectory);
        }

        public static IEnumerable<IPackageFile> GetLibFiles(this IPackage package)
        {
            return package.GetFiles(Constants.LibDirectory);
        }

        /// <summary>
        /// Returns the list of files from a satellite package that are considered satellite files.
        /// </summary>
        /// <remarks>
        /// This method must only be called for packages that specify a language
        /// </remarks>
        /// <param name="package">The package to get satellite files from.</param>
        /// <returns>The list of satellite files, which may be an empty list.</returns>
        public static IEnumerable<IPackageFile> GetSatelliteFiles(this IPackage package)
        {
            if (String.IsNullOrEmpty(package.Language))
            {
                return Enumerable.Empty<IPackageFile>();
            }

            // Satellite files are those within the Lib folder that have a culture-specific subfolder anywhere in the path
            return package.GetLibFiles().Where(file => Path.GetDirectoryName(file.Path).Split(Path.DirectorySeparatorChar)
                                                                .Contains(package.Language, StringComparer.OrdinalIgnoreCase));
        }

        public static IEnumerable<PackageIssue> Validate(this IPackage package, IEnumerable<IPackageRule> rules)
        {
            if (package == null)
            {
                return null;
            }

            if (rules == null)
            {
                throw new ArgumentNullException("rules");
            }

            return rules.Where(r => r != null).SelectMany(r => r.Validate(package));
        }

        public static string GetHash(this IPackage package, string hashAlgorithm)
        {
            return GetHash(package, new CryptoHashProvider(hashAlgorithm));
        }

        public static string GetHash(this IPackage package, IHashProvider hashProvider)
        {
            using (Stream stream = package.GetStream())
            {
                byte[] packageBytes = stream.ReadAllBytes();
                return Convert.ToBase64String(hashProvider.CalculateHash(packageBytes));
            }
        }

        /// <summary>
        /// Returns true if a package has no content that applies to a project.
        /// </summary>
        public static bool HasProjectContent(this IPackage package)
        {
            // Note that while checking for both AssemblyReferences and LibFiles seems redundant,
            // there are tests that directly inject items into the AssemblyReferences collection
            // without having those files represented in the Lib folder.  We keep the redundant
            // check to play it on the safe side.

            return package.FrameworkAssemblies.Any() ||
                   package.AssemblyReferences.Any() ||
                   package.GetContentFiles().Any() ||
                   package.GetLibFiles().Any();
        }

        public static IEnumerable<FrameworkName> GetSupportedFrameworks(this IPackage package)
        {
            // The supported frameworks of a package is the union of the supported frameworks
            // of Content/Lib/Tools folders and those of Framework Assemblies.
            return package.FrameworkAssemblies
                          .SelectMany(a => a.SupportedFrameworks)
                          .Concat(package.GetFiles().SelectMany(a => a.SupportedFrameworks))
                          .Distinct();
        }

        public static IEnumerable<PackageDependency> GetCompatiblePackageDependencies(this IPackageMetadata package, FrameworkName targetFramework)
        {
            IEnumerable<PackageDependencySet> compatibleDependencySets;
            if (targetFramework == null)
            {
                compatibleDependencySets = package.DependencySets;
            }
            else if (!VersionUtility.TryGetCompatibleItems(targetFramework, package.DependencySets, out compatibleDependencySets))
            {
                compatibleDependencySets = new PackageDependencySet[0];
            }

            return compatibleDependencySets.SelectMany(d => d.Dependencies);
        }

        /// <summary>
        /// Returns true if a package has dependencies but no files.
        /// </summary>
        public static bool IsDependencyOnly(this IPackage package)
        {
            return !package.GetFiles().Any() && 
                   package.DependencySets.SelectMany(d => d.Dependencies).Any();
        }

        public static string GetFullName(this IPackageMetadata package)
        {
            return package.Id + " " + package.Version;
        }

        /// <summary>
        /// Calculates the canonical list of operations.
        /// </summary>
        public static IList<PackageOperation> Reduce(this IEnumerable<PackageOperation> operations)
        {
            // Convert the list of operations to a dictionary from (Action, Id, Version) -> [Operations]
            // We keep track of the index so that we preserve the ordering of the operations
            var operationLookup = operations.Select((o, index) => new { Operation = o, Index = index })
                                            .ToLookup(o => GetOperationKey(o.Operation))
                                            .ToDictionary(g => g.Key,
                                                          g => g.ToList());

            // Given a list of operations we're going to eliminate the ones that have opposites (i.e. 
            // if the list contains +A 1.0 and -A 1.0, then we eliminate them both entries).
            foreach (var operation in operations)
            {
                // We get the opposing operation for the current operation:
                // if o is +A 1.0 then the opposing key is - A 1.0
                Tuple<PackageAction, string, SemanticVersion> opposingKey = GetOpposingOperationKey(operation);

                // We can't use TryGetValue since the value of the dictionary entry
                // is a List of an anonymous type.
                if (operationLookup.ContainsKey(opposingKey))
                {
                    // If we find an opposing entry, we remove it from the list of candidates
                    var opposingOperations = operationLookup[opposingKey];
                    opposingOperations.RemoveAt(0);

                    // Remove the list from the dictionary if nothing is in it
                    if (!opposingOperations.Any())
                    {
                        operationLookup.Remove(opposingKey);
                    }
                }
            }

            // Create the final list of operations and order them by their original index
            return operationLookup.SelectMany(o => o.Value)
                                  .OrderBy(o => o.Index)
                                  .Select(o => o.Operation)
                                  .ToList();
        }

        private static Tuple<PackageAction, string, SemanticVersion> GetOperationKey(PackageOperation operation)
        {
            return Tuple.Create(operation.Action, operation.Package.Id, operation.Package.Version);
        }

        private static Tuple<PackageAction, string, SemanticVersion> GetOpposingOperationKey(PackageOperation operation)
        {
            return Tuple.Create(operation.Action == PackageAction.Install ?
                                PackageAction.Uninstall :
                                PackageAction.Install, operation.Package.Id, operation.Package.Version);
        }

        /// <summary>
        /// Returns a distinct set of elements using the comparer specified. This implementation will pick the last occurrence
        /// of each element instead of picking the first. This method assumes that similar items occur in order.
        /// </summary>
        public static IEnumerable<IPackage> AsCollapsed(this IEnumerable<IPackage> source)
        {
            return source.DistinctLast(PackageEqualityComparer.Id, PackageComparer.Version);
        }


        /// <summary>
        /// Collapses the packages by Id picking up the highest version for each Id that it encounters
        /// </summary>
        internal static IEnumerable<IPackage> CollapseById(this IEnumerable<IPackage> source)
        {
            return source.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                         .Select(g => g.OrderByDescending(p => p.Version).First());
        }

        public static IEnumerable<IPackage> FilterByPrerelease(this IEnumerable<IPackage> packages, bool allowPrerelease)
        {
            if (packages == null)
            {
                return null;
            }

            if (!allowPrerelease)
            {
                packages = packages.Where(p => p.IsReleaseVersion());
            }

            return packages;
        }

        /// <summary>
        /// Returns packages where the search text appears in the default set of properties to search. The default set includes Id, Description and Tags.
        /// </summary>
        public static IQueryable<T> Find<T>(this IQueryable<T> packages, string searchText) where T : IPackage
        {
            return Find(packages, _packagePropertiesToSearch, searchText);
        }

        /// <summary>
        /// Returns packages where the search text appears in any of the properties to search. 
        /// Note that not all properties can be successfully queried via this method particularly over a OData feed. Verify indepedently if it works for the properties that need to be searched.
        /// </summary>
        public static IQueryable<T> Find<T>(this IQueryable<T> packages, IEnumerable<string> propertiesToSearch, string searchText) where T : IPackage
        {
            if (propertiesToSearch.IsEmpty())
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "propertiesToSearch");
            }

            if (String.IsNullOrEmpty(searchText))
            {
                return packages;
            }
            return Find(packages, propertiesToSearch, searchText.Split());
        }

        private static IQueryable<T> Find<T>(this IQueryable<T> packages, IEnumerable<string> propertiesToSearch, IEnumerable<string> searchTerms) where T : IPackage
        {
            if (!searchTerms.Any())
            {
                return packages;
            }

            IEnumerable<string> nonNullTerms = searchTerms.Where(s => s != null);
            if (!nonNullTerms.Any())
            {
                return packages;
            }

            return packages.Where(BuildSearchExpression<T>(propertiesToSearch, nonNullTerms));
        }

        /// <summary>
        /// Constructs an expression to search for individual tokens in a search term in the Id and Description of packages
        /// </summary>
        private static Expression<Func<T, bool>> BuildSearchExpression<T>(IEnumerable<string> propertiesToSearch, IEnumerable<string> searchTerms) where T : IPackage
        {
            Debug.Assert(searchTerms != null);
            var parameterExpression = Expression.Parameter(typeof(IPackageMetadata));
            // package.Id.ToLower().Contains(term1) || package.Id.ToLower().Contains(term2)  ...
            Expression condition = (from term in searchTerms
                                    from property in propertiesToSearch
                                    select BuildExpressionForTerm(parameterExpression, term, property)).Aggregate(Expression.OrElse);
            return Expression.Lambda<Func<T, bool>>(condition, parameterExpression);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower",
            Justification = "The expression is remoted using Odata which does not support the culture parameter")]
        private static Expression BuildExpressionForTerm(ParameterExpression packageParameterExpression, string term, string propertyName)
        {
            // For tags we want to prepend and append spaces to do an exact match
            if (propertyName.Equals(TagsProperty, StringComparison.OrdinalIgnoreCase))
            {
                term = " " + term + " ";
            }

            MethodInfo stringContains = typeof(String).GetMethod("Contains", new Type[] { typeof(string) });
            MethodInfo stringToLower = typeof(String).GetMethod("ToLower", Type.EmptyTypes);

            // package.Id / package.Description
            var propertyExpression = Expression.Property(packageParameterExpression, propertyName);
            // .ToLower()
            var toLowerExpression = Expression.Call(propertyExpression, stringToLower);

            // Handle potentially null properties
            // package.{propertyName} != null && package.{propertyName}.ToLower().Contains(term.ToLower())
            return Expression.AndAlso(Expression.NotEqual(propertyExpression,
                                                      Expression.Constant(null)),
                                      Expression.Call(toLowerExpression, stringContains, Expression.Constant(term.ToLower())));
        }
    }
}