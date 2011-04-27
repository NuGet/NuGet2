using System;
using System.Linq;

namespace NuGet {
    public static class PackageSourceProviderExtensions {
        public static IPackageRepository GetAggregate(this IPackageSourceProvider provider, IPackageRepositoryFactory factory) {
            return new AggregateRepository(provider.LoadPackageSources().Select(s => factory.CreateRepository(s.Source)));
        }

        public static string ResolveSource(this IPackageSourceProvider provider, string name) {
            return provider.LoadPackageSources()
                           .Where(s => s.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                           .Select(s => s.Source)
                           .FirstOrDefault();
        }
    }
}