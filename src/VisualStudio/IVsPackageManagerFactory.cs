namespace NuGet.VisualStudio
{
    public interface IVsPackageManagerFactory
    {
        IVsPackageManager CreatePackageManager();

        IVsPackageManager CreatePackageManager(IPackageRepository repository, bool useFallbackForDependencies);

        IVsPackageManager CreatePackageManagerToManageInstalledPackages();

        IVsPackageManager CreatePackageManagerWithAllPackageSources();
    }
}