namespace NuGet.VisualStudio {
    public interface IVsPackageManagerFactory {
        IVsPackageManager CreatePackageManager(bool useFallbackForDependencies);

        IVsPackageManager CreatePackageManager(IPackageRepository repository, bool useFallbackForDependencies);
    }
}
