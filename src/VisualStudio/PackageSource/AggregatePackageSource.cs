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

        // IMPORTANT: do NOT remove this method. It is used by functional tests.
        public static IEnumerable<PackageSource> GetEnabledPackageSourcesWithAggregate()
        {
            return GetEnabledPackageSourcesWithAggregate(ServiceLocator.GetInstance<IVsPackageSourceProvider>());
        }

        public static IEnumerable<PackageSource> GetEnabledPackageSourcesWithAggregate(this IPackageSourceProvider provider)
        {
            var packageSources = provider.GetEnabledPackageSources().ToArray();

            // If there's less than 2 package sources, don't add the Aggregate source because it will be exactly the same as the main source.
            if (packageSources.Length <= 1)
            {
                return packageSources;
            }

            return new[] { Instance }.Concat(packageSources);
        }
    }
}