using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio
{
    public static class AggregatePackageSource
    {
        public static readonly PackageSource Instance = new PackageSource("(Aggregate source)", Resources.VsResources.AggregateSourceName);

        public static bool IsAggregate(this PackageSource source)
        {
            return source == Instance;
        }

        public static IEnumerable<PackageSource> GetEnabledPackageSourcesWithAggregate(this IPackageSourceProvider provider)
        {
            return new[] { Instance }.Concat(provider.GetEnabledPackageSources());
        }

        public static IEnumerable<PackageSource> GetEnabledPackageSourcesWithAggregate()
        {
            return GetEnabledPackageSourcesWithAggregate(ServiceLocator.GetInstance<IVsPackageSourceProvider>());
        }
    }
}