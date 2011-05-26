using System;
using System.Linq;

namespace NuGet {
    public static class PackageSourceProviderExtensions {
        public static AggregateRepository GetAggregate(this IPackageSourceProvider provider, IPackageRepositoryFactory factory) {
            return GetAggregate(provider, factory, ignoreFailingRepositories: false);
        }

        public static AggregateRepository GetAggregate(this IPackageSourceProvider provider, IPackageRepositoryFactory factory, bool ignoreFailingRepositories) {
            return new AggregateRepository(factory, provider.LoadPackageSources().Select(s => s.Source), ignoreFailingRepositories);
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