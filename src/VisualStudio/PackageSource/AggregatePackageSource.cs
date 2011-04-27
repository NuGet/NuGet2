
namespace NuGet.VisualStudio {
    public static class AggregatePackageSource {
        public static readonly PackageSource Instance = new PackageSource("(Aggregate source)", Resources.VsResources.AggregateSourceName);

        public static bool IsAggregate(this PackageSource source) {
            return source == Instance;
        }
    }
}