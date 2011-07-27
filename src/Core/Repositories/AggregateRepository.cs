using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NuGet {
    public class AggregateRepository : PackageRepositoryBase, IPackageLookup, IDependencyResolver, ISearchableRepository {
        /// <summary>
        /// When the ignore flag is set up, this collection keeps track of failing repositories so that the AggregateRepository 
        /// does not query them again.
        /// </summary>
        private readonly ConcurrentBag<IPackageRepository> _failingRepositories = new ConcurrentBag<IPackageRepository>();
        private readonly IEnumerable<IPackageRepository> _repositories;
        private const string SourceValue = "(Aggregate source)";
        private ILogger _logger;

        public override string Source {
            get { return SourceValue; }
        }

        public ILogger Logger {
            get { return _logger ?? NullLogger.Instance; }
            set { _logger = value; }
        }

        /// <summary>
        /// Determines if dependency resolution is performed serially on a per-repository basis. The first repository that has a compatible dependency 
        /// regardless of version would win if this property is true.
        /// </summary>
        public bool ResolveDependenciesVertically { get; set; }

        public bool IgnoreFailingRepositories { get; set; }

        /// <remarks>
        /// Iterating over Repositories returned by this property may throw regardless of IgnoreFailingRepositories.
        /// </remarks>
        public IEnumerable<IPackageRepository> Repositories {
            get { return _repositories; }
        }

        public AggregateRepository(IEnumerable<IPackageRepository> repositories) {
            if (repositories == null) {
                throw new ArgumentNullException("repositories");
            }
            _repositories = repositories;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to suppress any exception that we may encounter.")]
        public AggregateRepository(IPackageRepositoryFactory repositoryFactory, IEnumerable<string> packageSources, bool ignoreFailingRepositories) {
            IgnoreFailingRepositories = ignoreFailingRepositories;
            Func<string, IPackageRepository> createRepository = repositoryFactory.CreateRepository;
            if (ignoreFailingRepositories) {
                createRepository = (source) => {
                    try {
                        return repositoryFactory.CreateRepository(source);
                    }
                    catch {
                        return null;
                    }
                };
            }

            _repositories = (from source in packageSources
                             let repository = createRepository(source)
                             where repository != null
                             select repository).ToArray();

        }

        public override IQueryable<IPackage> GetPackages() {
            // We need to follow this pattern in all AggregateRepository methods to ensure it suppresses exceptions that may occur if the Ignore flag is set.  Oh how I despise my code. 
            var defaultResult = Enumerable.Empty<IPackage>().AsSafeQueryable();
            Func<IPackageRepository, IQueryable<IPackage>> getPackages = Wrap(r => r.GetPackages(), defaultResult);
            return CreateAggregateQuery(Repositories.Select(getPackages));
        }

        public IPackage FindPackage(string packageId, Version version) {
            // When we're looking for an exact package, we can optimize but searching each
            // repository one by one until we find the package that matches.
            Func<IPackageRepository, IPackage> findPackage = Wrap(r => r.FindPackage(packageId, version));
            return Repositories.Select(findPackage)
                               .FirstOrDefault(p => p != null);
        }

        public IPackage ResolveDependency(PackageDependency dependency, IPackageConstraintProvider constraintProvider) {
            if (ResolveDependenciesVertically) {
                Func<IPackageRepository, IPackage> resolveDependency = Wrap(r => r.ResolveDependency(dependency, constraintProvider));

                var tasks = Repositories.Select(r => Task.Factory.StartNew(() => resolveDependency(r)))
                                   .ToArray();
                var completedTask = tasks.WhenAny(t => t != null);
                return tasks.WhenAny(t => t != null);
            }
            return this.ResolveDependencyCore(dependency, constraintProvider);
        }

        private Func<IPackageRepository, T> Wrap<T>(Func<IPackageRepository, T> factory, T defaultValue = null) where T : class {
            if (IgnoreFailingRepositories) {
                return repository => {
                    if (_failingRepositories.Contains(repository)) {
                        return defaultValue;
                    }

                    try {
                        return factory(repository);
                    }
                    catch (Exception ex) {
                        LogRepository(repository, ex);
                        return defaultValue;
                    }
                };
            }
            return factory;
        }

        private void LogRepository(IPackageRepository repository, Exception ex) {
            _failingRepositories.Add(repository);
            Logger.Log(MessageLevel.Warning, ExceptionUtility.Unwrap(ex).Message);
        }


        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks) {
            return CreateAggregateQuery(Repositories.Select(r => r.Search(searchTerm, targetFrameworks)));
        }

        private AggregateQuery<IPackage> CreateAggregateQuery(IEnumerable<IQueryable<IPackage>> queries) {
            return new AggregateQuery<IPackage>(queries,
                                                PackageEqualityComparer.IdAndVersion,
                                                Logger,
                                                IgnoreFailingRepositories);
        }
    }
}
