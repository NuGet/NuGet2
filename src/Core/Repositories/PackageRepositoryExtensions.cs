using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Versioning;

namespace NuGet
{
    public static class PackageRepositoryExtensions
    {
        public static IDisposable StartOperation(this IPackageRepository self, string operation)
        {
            IOperationAwareRepository repo = self as IOperationAwareRepository;
            if (repo != null)
            {
                return repo.StartOperation(operation);
            }
            return DisposableAction.NoOp;
        }

        public static bool Exists(this IPackageRepository repository, IPackageMetadata package)
        {
            return repository.Exists(package.Id, package.Version);
        }

        public static bool Exists(this IPackageRepository repository, string packageId)
        {
            return Exists(repository, packageId, version: null);
        }

        public static bool Exists(this IPackageRepository repository, string packageId, SemanticVersion version)
        {
            IPackageLookup packageLookup = repository as IPackageLookup;
            if ((packageLookup != null) && !String.IsNullOrEmpty(packageId) && (version != null))
            {
                return packageLookup.Exists(packageId, version);
            }
            return repository.FindPackage(packageId, version) != null;
        }

        public static bool TryFindPackage(this IPackageRepository repository, string packageId, SemanticVersion version, out IPackage package)
        {
            package = repository.FindPackage(packageId, version);
            return package != null;
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId)
        {
            return repository.FindPackage(packageId, version: null);
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, SemanticVersion version)
        {
            // Default allow pre release versions to true here because the caller typically wants to find all packages in this scenario for e.g when checking if a 
            // a package is already installed in the local repository. The same applies to allowUnlisted.
            return FindPackage(repository, packageId, version, NullConstraintProvider.Instance, allowPrereleaseVersions: true, allowUnlisted: true);
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, SemanticVersion version, bool allowPrereleaseVersions, bool allowUnlisted)
        {
            return FindPackage(repository, packageId, version, NullConstraintProvider.Instance, allowPrereleaseVersions, allowUnlisted);
        }

        public static IPackage FindPackage(
            this IPackageRepository repository,
            string packageId,
            SemanticVersion version,
            IPackageConstraintProvider constraintProvider,
            bool allowPrereleaseVersions,
            bool allowUnlisted)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            if (packageId == null)
            {
                throw new ArgumentNullException("packageId");
            }

            // if an explicit version is specified, disregard the 'allowUnlisted' argument
            // and always allow unlisted packages.
            if (version != null)
            {
                allowUnlisted = true;
            }

            // If the repository implements it's own lookup then use that instead.
            // This is an optimization that we use so we don't have to enumerate packages for
            // sources that don't need to.
            var packageLookup = repository as IPackageLookup;
            if (packageLookup != null && version != null)
            {
                return packageLookup.FindPackage(packageId, version);
            }

            IEnumerable<IPackage> packages = repository.FindPackagesById(packageId);

            packages = packages.ToList()
                               .OrderByDescending(p => p.Version);

            if (!allowUnlisted)
            {
                packages = packages.Where(PackageExtensions.IsListed);
            }

            if (version != null)
            {
                packages = packages.Where(p => p.Version == version);
            }
            else if (constraintProvider != null)
            {
                packages = FilterPackagesByConstraints(constraintProvider, packages, packageId, allowPrereleaseVersions);
            }

            return packages.FirstOrDefault();
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, IVersionSpec versionSpec,
                IPackageConstraintProvider constraintProvider, bool allowPrereleaseVersions, bool allowUnlisted)
        {
            var packages = repository.FindPackages(packageId, versionSpec, allowPrereleaseVersions, allowUnlisted);

            if (constraintProvider != null)
            {
                packages = FilterPackagesByConstraints(constraintProvider, packages, packageId, allowPrereleaseVersions);
            }

            return packages.FirstOrDefault();
        }

        public static IEnumerable<IPackage> FindPackages(this IPackageRepository repository, IEnumerable<string> packageIds)
        {
            if (packageIds == null)
            {
                throw new ArgumentNullException("packageIds");
            }

            return FindPackages(repository, packageIds, GetFilterExpression);
        }

        public static IEnumerable<IPackage> FindPackagesById(this IPackageRepository repository, string packageId)
        {
            var serviceBasedRepository = repository as IServiceBasedRepository;
            if (serviceBasedRepository != null)
            {
                return serviceBasedRepository.FindPackagesById(packageId).ToList();
            }
            else
            {
                return FindPackagesByIdCore(repository, packageId);
            }
        }

        internal static IEnumerable<IPackage> FindPackagesByIdCore(IPackageRepository repository, string packageId)
        {
            var cultureRepository = repository as ICultureAwareRepository;
            if (cultureRepository != null)
            {
                packageId = packageId.ToLower(cultureRepository.Culture);
            }
            else
            {
                packageId = packageId.ToLower(CultureInfo.CurrentCulture);
            }

            return (from p in repository.GetPackages()
                    where p.Id.ToLower() == packageId
                    orderby p.Id
                    select p).ToList();
        }

        /// <summary>
        /// Since Odata dies when our query for updates is too big. We query for updates 10 packages at a time
        /// and return the full list of packages.
        /// </summary>
        private static IEnumerable<IPackage> FindPackages<T>(this IPackageRepository repository, IEnumerable<T> items, Func<IEnumerable<T>,
                Expression<Func<IPackage, bool>>> filterSelector)
        {
            const int batchSize = 10;

            while (items.Any())
            {
                IEnumerable<T> currentItems = items.Take(batchSize);
                Expression<Func<IPackage, bool>> filterExpression = filterSelector(currentItems);

                var query = repository.GetPackages().Where(filterExpression).OrderBy(p => p.Id);
                foreach (var package in query)
                {
                    yield return package;
                }

                items = items.Skip(batchSize);
            }
        }

        public static IEnumerable<IPackage> FindPackages(
            this IPackageRepository repository,
            string packageId,
            IVersionSpec versionSpec,
            bool allowPrereleaseVersions,
            bool allowUnlisted)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            if (packageId == null)
            {
                throw new ArgumentNullException("packageId");
            }

            IEnumerable<IPackage> packages = repository.FindPackagesById(packageId)
                                                       .OrderByDescending(p => p.Version);

            if (!allowUnlisted)
            {
                packages = packages.Where(PackageExtensions.IsListed);
            }

            if (versionSpec != null)
            {
                packages = packages.FindByVersion(versionSpec);
            }

            packages = FilterPackagesByConstraints(NullConstraintProvider.Instance, packages, packageId, allowPrereleaseVersions);

            return packages;
        }

        public static IPackage FindPackage(
            this IPackageRepository repository,
            string packageId,
            IVersionSpec versionSpec,
            bool allowPrereleaseVersions,
            bool allowUnlisted)
        {
            return repository.FindPackages(packageId, versionSpec, allowPrereleaseVersions, allowUnlisted).FirstOrDefault();
        }

        public static IEnumerable<IPackage> FindCompatiblePackages(this IPackageRepository repository,
                                                                   IPackageConstraintProvider constraintProvider,
                                                                   IEnumerable<string> packageIds,
                                                                   IPackage package,
                                                                   FrameworkName targetFramework,
                                                                   bool allowPrereleaseVersions)
        {
            return (from p in repository.FindPackages(packageIds)
                    where allowPrereleaseVersions || p.IsReleaseVersion()
                    let dependency = p.FindDependency(package.Id, targetFramework)
                    let otherConstaint = constraintProvider.GetConstraint(p.Id)
                    where dependency != null &&
                          dependency.VersionSpec.Satisfies(package.Version) &&
                          (otherConstaint == null || otherConstaint.Satisfies(package.Version))
                    select p);
        }

        public static PackageDependency FindDependency(this IPackageMetadata package, string packageId, FrameworkName targetFramework)
        {
            return (from dependency in package.GetCompatiblePackageDependencies(targetFramework)
                    where dependency.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)
                    select dependency).FirstOrDefault();
        }

        public static IQueryable<IPackage> Search(this IPackageRepository repository, string searchTerm, bool allowPrereleaseVersions)
        {
            return Search(repository, searchTerm, targetFrameworks: Enumerable.Empty<string>(), allowPrereleaseVersions: allowPrereleaseVersions);
        }

        public static IQueryable<IPackage> Search(this IPackageRepository repository, string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions)
        {
            if (targetFrameworks == null)
            {
                throw new ArgumentNullException("targetFrameworks");
            }

            var serviceBasedRepository = repository as IServiceBasedRepository;
            if (serviceBasedRepository != null)
            {
                return serviceBasedRepository.Search(searchTerm, targetFrameworks, allowPrereleaseVersions);
            }

            // Ignore the target framework if the repository doesn't support searching
            return repository.GetPackages().Find(searchTerm)
                                           .FilterByPrerelease(allowPrereleaseVersions)
                                           .AsQueryable();
        }

        public static IPackage ResolveDependency(this IPackageRepository repository, PackageDependency dependency, bool allowPrereleaseVersions, bool preferListedPackages)
        {
            return ResolveDependency(repository, dependency, constraintProvider: null, allowPrereleaseVersions: allowPrereleaseVersions, preferListedPackages: preferListedPackages);
        }

        public static IPackage ResolveDependency(this IPackageRepository repository, PackageDependency dependency, IPackageConstraintProvider constraintProvider, bool allowPrereleaseVersions, bool preferListedPackages)
        {
            IDependencyResolver dependencyResolver = repository as IDependencyResolver;
            if (dependencyResolver != null)
            {
                return dependencyResolver.ResolveDependency(dependency, constraintProvider, allowPrereleaseVersions, preferListedPackages);
            }
            return ResolveDependencyCore(repository, dependency, constraintProvider, allowPrereleaseVersions, preferListedPackages);
        }

        internal static IPackage ResolveDependencyCore(
            this IPackageRepository repository,
            PackageDependency dependency,
            IPackageConstraintProvider constraintProvider,
            bool allowPrereleaseVersions,
            bool preferListedPackages)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            if (dependency == null)
            {
                throw new ArgumentNullException("dependency");
            }

            IEnumerable<IPackage> packages = repository.FindPackagesById(dependency.Id).ToList();

            // Always filter by constraints when looking for dependencies
            packages = FilterPackagesByConstraints(constraintProvider, packages, dependency.Id, allowPrereleaseVersions);

            IList<IPackage> candidates = packages.ToList();

            if (preferListedPackages)
            {
                // pick among Listed packages first
                IPackage listedSelectedPackage = ResolveDependencyCore(candidates.Where(PackageExtensions.IsListed),
                                                                       dependency);
                if (listedSelectedPackage != null)
                {
                    return listedSelectedPackage;
                }
            }

            return ResolveDependencyCore(candidates, dependency);
        }

        private static IPackage ResolveDependencyCore(IEnumerable<IPackage> packages, PackageDependency dependency)
        {
            // If version info was specified then use it
            if (dependency.VersionSpec != null)
            {
                packages = packages.FindByVersion(dependency.VersionSpec);
                return packages.ResolveSafeVersion();
            }
            else
            {
                // BUG 840: If no version info was specified then pick the latest
                return packages.OrderByDescending(p => p.Version)
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// Returns updates for packages from the repository 
        /// </summary>
        /// <param name="repository">The repository to search for updates</param>
        /// <param name="packages">Packages to look for updates</param>
        /// <param name="includePrerelease">Indicates whether to consider prerelease updates.</param>
        /// <param name="includeAllVersions">Indicates whether to include all versions of an update as opposed to only including the latest version.</param>
        public static IEnumerable<IPackage> GetUpdates(
            this IPackageRepository repository,
            IEnumerable<IPackage> packages,
            bool includePrerelease,
            bool includeAllVersions,
            IEnumerable<FrameworkName> targetFramework = null)
        {
            var serviceBasedRepository = repository as IServiceBasedRepository;
            return serviceBasedRepository != null ? serviceBasedRepository.GetUpdates(packages, includePrerelease, includeAllVersions, targetFramework) :
                                                    repository.GetUpdatesCore(packages, includePrerelease, includeAllVersions, targetFramework);
        }

        public static IEnumerable<IPackage> GetUpdatesCore(this IPackageRepository repository, IEnumerable<IPackageMetadata> packages, bool includePrerelease, bool includeAllVersions, 
            IEnumerable<FrameworkName> targetFramework)
        {
            List<IPackageMetadata> packageList = packages.ToList();

            if (!packageList.Any())
            {
                return Enumerable.Empty<IPackage>();
            }


            // These are the packages that we need to look at for potential updates.
            ILookup<string, IPackage> sourcePackages = GetUpdateCandidates(repository, packageList, includePrerelease)
                                                                            .ToList()
                                                                            .ToLookup(package => package.Id, StringComparer.OrdinalIgnoreCase);

            var updates = from package in packageList
                          from candidate in sourcePackages[package.Id]
                          where (candidate.Version > package.Version) &&
                                 SupportsTargetFrameworks(targetFramework, candidate)
                          select candidate;

            if (!includeAllVersions)
            {
                return updates.CollapseById();
            }
            return updates;
        }

        private static bool SupportsTargetFrameworks(IEnumerable<FrameworkName> targetFramework, IPackage package)
        {
            return targetFramework.IsEmpty() || targetFramework.Any(t => VersionUtility.IsCompatible(t, package.GetSupportedFrameworks()));
        }

        public static IPackageRepository Clone(this IPackageRepository repository)
        {
            var cloneableRepository = repository as ICloneableRepository;
            if (cloneableRepository != null)
            {
                return cloneableRepository.Clone();
            }
            return repository;
        }

        /// <summary>
        /// Since odata dies when our query for updates is too big. We query for updates 10 packages at a time
        /// and return the full list of candidates for updates.
        /// </summary>
        private static IEnumerable<IPackage> GetUpdateCandidates(
            IPackageRepository repository,
            IEnumerable<IPackageMetadata> packages,
            bool includePrerelease)
        {

            var query = FindPackages(repository, packages, GetFilterExpression);
            if (!includePrerelease)
            {
                query = query.Where(p => p.IsReleaseVersion());
            }

            // for updates, we never consider unlisted packages
            query = query.Where(PackageExtensions.IsListed);

            return query;
        }

        /// <summary>
        /// For the list of input packages generate an expression like:
        /// p => p.Id == 'package1id' or p.Id == 'package2id' or p.Id == 'package3id'... up to package n
        /// </summary>
        private static Expression<Func<IPackage, bool>> GetFilterExpression(IEnumerable<IPackageMetadata> packages)
        {
            return GetFilterExpression(packages.Select(p => p.Id));
        }

        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower", Justification = "This is for a linq query")]
        private static Expression<Func<IPackage, bool>> GetFilterExpression(IEnumerable<string> ids)
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(IPackageMetadata));
            Expression expressionBody = ids.Select(id => GetCompareExpression(parameterExpression, id.ToLower()))
                                                .Aggregate(Expression.OrElse);

            return Expression.Lambda<Func<IPackage, bool>>(expressionBody, parameterExpression);
        }

        /// <summary>
        /// Builds the expression: package.Id.ToLower() == "somepackageid"
        /// </summary>
        private static Expression GetCompareExpression(Expression parameterExpression, object value)
        {
            // package.Id
            Expression propertyExpression = Expression.Property(parameterExpression, "Id");
            // .ToLower()
            Expression toLowerExpression = Expression.Call(propertyExpression, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            // == localPackage.Id
            return Expression.Equal(toLowerExpression, Expression.Constant(value));
        }

        private static IEnumerable<IPackage> FilterPackagesByConstraints(
            IPackageConstraintProvider constraintProvider,
            IEnumerable<IPackage> packages,
            string packageId,
            bool allowPrereleaseVersions)
        {
            constraintProvider = constraintProvider ?? NullConstraintProvider.Instance;

            // Filter packages by this constraint
            IVersionSpec constraint = constraintProvider.GetConstraint(packageId);
            if (constraint != null)
            {
                packages = packages.FindByVersion(constraint);
            }
            if (!allowPrereleaseVersions)
            {
                packages = packages.Where(p => p.IsReleaseVersion());
            }

            return packages;
        }

        internal static IPackage ResolveSafeVersion(this IEnumerable<IPackage> packages)
        {
            // Return null if there's no packages
            if (packages == null || !packages.Any())
            {
                return null;
            }

            // We want to take the biggest build and revision number for the smallest
            // major and minor combination (we want to make some versioning assumptions that the 3rd number is a non-breaking bug fix). This is so that we get the closest version
            // to the dependency, but also get bug fixes without requiring people to manually update the nuspec.
            // For example, if A -> B 1.0.0 and the feed has B 1.0.0 and B 1.0.1 then the more correct choice is B 1.0.1. 
            // If we don't do this, A will always end up getting the 'buggy' 1.0.0, 
            // unless someone explicitly changes it to ask for 1.0.1, which is very painful if many packages are using B 1.0.0.
            var groups = from p in packages
                         group p by new { p.Version.Version.Major, p.Version.Version.Minor } into g
                         orderby g.Key.Major, g.Key.Minor
                         select g;

            return (from p in groups.First()
                    orderby p.Version descending
                    select p).FirstOrDefault();
        }
    }
}