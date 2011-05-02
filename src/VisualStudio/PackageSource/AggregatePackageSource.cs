using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio {
    public static class AggregatePackageSource {
        public static readonly PackageSource Instance = new PackageSource("(Aggregate source)", Resources.VsResources.AggregateSourceName);

        public static bool IsAggregate(this PackageSource source) {
            return source == Instance;
        }

        public static IEnumerable<PackageSource> GetPackageSourcesWithAggregate(this IPackageSourceProvider provider) {
            return new[] { Instance }.Concat(provider.LoadPackageSources());
        }

        public static IEnumerable<PackageSource> GetPackageSourcesWithAggregate() {
            return GetPackageSourcesWithAggregate(ServiceLocator.GetInstance<IVsPackageSourceProvider>());
        }
    }
}