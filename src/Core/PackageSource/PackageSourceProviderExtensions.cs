using System;
using System.Linq;
using System.Collections.Generic;

namespace NuGet {
    public static class PackageSourceProviderExtensions {
        public static IPackageRepository GetAggregate(this IPackageSourceProvider provider, IPackageRepositoryFactory factory) {
            return GetAggregate(provider, factory, ignoreInvalidRepositories: false);
        }

        public static IPackageRepository GetAggregate(this IPackageSourceProvider provider, IPackageRepositoryFactory factory, bool ignoreInvalidRepositories) {
            Func<string, IPackageRepository> createRepository = factory.CreateRepository;

            if (ignoreInvalidRepositories) {
                createRepository = (source) => {
                    try {
                        return factory.CreateRepository(source);
                    }
                    catch (InvalidOperationException) {
                        return null;
                    }
                };
            }

            var repositories = (from source in provider.LoadPackageSources()
                                let repository = createRepository(source.Source)
                                where repository != null
                                select repository).ToArray();

            return new AggregateRepository(repositories) { IgnoreInvalidRepositories = ignoreInvalidRepositories };
        }

        public static string ResolveSource(this IPackageSourceProvider provider, string name) {
            return provider.LoadPackageSources()
                           .Where(s => s.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                           .Select(s => s.Source)
                           .FirstOrDefault();
        }
    }
}