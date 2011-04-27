using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio {
    public static class AggregatePackageSource {
        public static readonly PackageSource Instance = new PackageSource("(Aggregate source)", Resources.VsResources.AggregateSourceName);

        public static bool IsAggregate(this PackageSource source) {
            return source == Instance;
        }

        public static IEnumerable<PackageSource> GetPackageSourcesWithAggregate(this IPackageSourceProvider provider) {
            return Enumerable.Repeat(Instance, 1).Concat(provider.LoadPackageSources());
        }
    }
}