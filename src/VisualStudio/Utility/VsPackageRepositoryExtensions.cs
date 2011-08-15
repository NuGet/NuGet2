using System;
using System.Linq;

namespace NuGet.VisualStudio {
    public static class VsPackageRepositoryExtensions {
        public static IPackageRepository Clone(this IPackageRepository repository) {
            try {
                return CloneInternal(repository);
            }
            catch {
                // If we throw, the original repository is failing. Return it as is so that the API using it can report errors when it encounters it.
                return repository;
            }
        }

        private static IPackageRepository CloneInternal(this IPackageRepository repository) {
            var dataServiceRepository = repository as DataServicePackageRepository;
            if (dataServiceRepository != null) {
                return new DataServicePackageRepository(new Uri(dataServiceRepository.Source));
            }

            var vsPackageRepository = repository as VsPackageSourceRepository;
            if (vsPackageRepository != null) {
                return Clone(vsPackageRepository.ActiveRepository);
            }

            var aggregateRepository = repository as AggregateRepository;
            if (aggregateRepository != null) {
                return new AggregateRepository(aggregateRepository.Repositories.Select(Clone));
            }

            return repository;
        }
    }
}
