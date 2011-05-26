using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NuGet {
    public class AggregateRepository : PackageRepositoryBase, IPackageLookup, IDependencyProvider {
        /// <summary>
        /// When the ignore flag is set up, this collection keeps track of failing repositories so that the AggregateRepository 
        /// does not query them again.
        /// </summary>
        private readonly ConcurrentBag<IPackageRepository> _failingRepositories = new ConcurrentBag<IPackageRepository>();
        private readonly IEnumerable<IPackageRepository> _repositories;
        private const string SourceValue = "(Aggregate source)";
        private ILogger _logger;

        public override string Source {
            get {
                return SourceValue;
            }
        }

        public ILogger Logger {
            get {
                return _logger ?? NullLogger.Instance;
            }
            set {
                _logger = value;
            }
        }

        public bool IgnoreFailingRepositories { get; set; }

        public IEnumerable<IPackageRepository> Repositories {
            get {
                if (IgnoreFailingRepositories) {
                    return EnumerableExtensions.SafeIterate(_repositories.Select(EnsureValid).Where(r => r != null));
                }
                return _repositories;
            }
        }

        public AggregateRepository(IEnumerable<IPackageRepository> repositories) {
            if (repositories == null) {
                throw new ArgumentNullException("repositories");
            }
            _repositories = repositories;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to suppress any exception that we may encounter.")]
        public override IQueryable<IPackage> GetPackages() {
            // We need to follow this pattern in all AggregateRepository methods to ensure it suppresses exceptions that may occur if the Ignore flag is set.  Oh how I despise my code. 
            Func<IPackageRepository, IQueryable<IPackage>> getPackages = r => r.GetPackages();
            if (IgnoreFailingRepositories) {
                getPackages = repo => {
                    var defaultResult = Enumerable.Empty<IPackage>().AsSafeQueryable();
                    if (_failingRepositories.Contains(repo)) {
                        return defaultResult;
                    }

                    try {
                        return repo.GetPackages();
                    }
                    catch (Exception ex) {
                        _failingRepositories.Add(repo);
                        LogRepository(repo, ex);
                        return defaultResult;
                    }
                };
            }
            return new AggregateQuery<IPackage>(Repositories.Select(getPackages),
                PackageEqualityComparer.IdAndVersion, Logger, IgnoreFailingRepositories);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to suppress any exception that we may encounter.")]
        public IPackage FindPackage(string packageId, Version version) {
            // When we're looking for an exact package, we can optimize but searching each
            // repository one by one until we find the package that matches.
            Func<IPackageRepository, IPackage> findPackage = r => r.FindPackage(packageId, version);
            if (IgnoreFailingRepositories) {
                findPackage = repo => {
                    if (_failingRepositories.Contains(repo)) {
                        return null;
                    }
                    try {
                        return repo.FindPackage(packageId, version);
                    }
                    catch (Exception ex) {
                        _failingRepositories.Add(repo);
                        LogRepository(repo, ex);
                        return null;
                    }
                };
            }
            return Repositories.Select(findPackage)
                               .FirstOrDefault(p => p != null);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to suppress any exception that we may encounter.")]
        public IQueryable<IPackage> GetDependencies(string packageId) {
            // An AggregateRepository needs to call GetDependencies on individual repositories in the event any one of them 
            // implements an IDependencyProvider.
            Func<IPackageRepository, IQueryable<IPackage>> getDependencies = r => r.GetDependencies(packageId);
            if (IgnoreFailingRepositories) {
                getDependencies = repo => {
                    var defaultResult = Enumerable.Empty<IPackage>().AsSafeQueryable();
                    if (_failingRepositories.Contains(repo)) {
                        return defaultResult;
                    }

                    try {
                        return repo.GetDependencies(packageId);
                    }
                    catch (Exception ex) {
                        _failingRepositories.Add(repo);
                        LogRepository(repo, ex);
                        return defaultResult;
                    }
                };
            }

            return Repositories.SelectMany(getDependencies)
                                .Distinct(PackageEqualityComparer.IdAndVersion)
                                .AsQueryable();
        }

        private IPackageRepository EnsureValid(IPackageRepository repository) {
            try {
                if (_failingRepositories.Contains(repository)) {
                    return null;
                }
                // For remote repositories with redirected http clients, trying to access Source is when it would attempt to resolve it and throw. We need to safely iterate it.
                (repository.Source ?? String.Empty).ToString();
                return repository;
            }
            catch (Exception ex) {
                LogRepository(repository, ex);
                throw;
            }
        }

        private void LogRepository(IPackageRepository repository, Exception ex) {
            _failingRepositories.Add(repository);
            Logger.Log(MessageLevel.Warning, (ex.InnerException ?? ex).Message);
        }

    }
}
