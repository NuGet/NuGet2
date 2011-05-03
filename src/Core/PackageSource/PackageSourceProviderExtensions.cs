using System;
using System.Linq;

namespace NuGet {
    public static class PackageSourceProviderExtensions {
        public static IPackageRepository GetAggregate(this IPackageSourceProvider provider, IPackageRepositoryFactory factory) {
            return GetAggregate(provider, factory, ignoreFailingRepositories: false);
        }

        public static IPackageRepository GetAggregate(this IPackageSourceProvider provider, IPackageRepositoryFactory factory, bool ignoreFailingRepositories) {
            Func<string, IPackageRepository> createRepository = factory.CreateRepository;

            if (ignoreFailingRepositories) {
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

            return new AggregateRepository(repositories) { IgnoreFailingRepositories = ignoreFailingRepositories };
        }

        /// <summary>
        /// Resolves a package source by either Name or Source.
        /// </summary>
        public static string ResolveSource(this IPackageSourceProvider provider, string value) {
            var resolvedSource = (from source in provider.LoadPackageSources()
                                    where source.Name.Equals(value, StringComparison.CurrentCultureIgnoreCase) || source.Source.Equals(value, StringComparison.OrdinalIgnoreCase)
                                    select source.Source
                                   ).FirstOrDefault();

            return resolvedSource ?? value;
        }
    }
}