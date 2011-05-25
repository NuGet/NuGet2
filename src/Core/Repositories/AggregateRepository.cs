using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NuGet {
    public class AggregateRepository : PackageRepositoryBase, IPackageLookup, IDependencyProvider {
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
            get { return _repositories; }
        }

        public AggregateRepository(IEnumerable<IPackageRepository> repositories) {
            if (repositories == null) {
                throw new ArgumentNullException("repositories");
            }
            _repositories = repositories;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to suppress any exception that we may encounter.")]
        public override IQueryable<IPackage> GetPackages() {
            Func<IPackageRepository, IQueryable<IPackage>> getPackages = r => r.GetPackages();
            if (IgnoreFailingRepositories) {
                getPackages = r => {
                    try {
                        return r.GetPackages();
                    }
                    catch (Exception ex) {
                        Logger.Log(MessageLevel.Warning, (ex.InnerException ?? ex).Message);
                        return Enumerable.Empty<IPackage>().AsQueryable();
                    }
                };
            }
            return new AggregateQuery<IPackage>(_repositories.Select(getPackages),
                PackageEqualityComparer.IdAndVersion, Logger, IgnoreFailingRepositories);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification="We want to suppress any exception that we may encounter.")]
        public IPackage FindPackage(string packageId, Version version) {
            // When we're looking for an exact package, we can optimize but searching each
            // repository one by one until we find the package that matches.
            Func<IPackageRepository, IPackage> findPackage = r => r.FindPackage(packageId, version);
            if (IgnoreFailingRepositories) {
                findPackage = r => {
                    try {
                        return r.FindPackage(packageId, version);
                    }
                    catch (Exception ex) {
                        Logger.Log(MessageLevel.Warning, (ex.InnerException ?? ex).Message);
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
                getDependencies = r => {
                    try {
                        return r.GetDependencies(packageId);
                    }
                    catch (Exception ex) {
                        Logger.Log(MessageLevel.Warning, (ex.InnerException ?? ex).Message);
                        return Enumerable.Empty<IPackage>().AsQueryable();
                    }
                };
            }

            return _repositories.SelectMany(getDependencies)
                                .Distinct(PackageEqualityComparer.IdAndVersion)
                                .AsQueryable();
        }
    }
}
