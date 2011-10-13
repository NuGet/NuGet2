namespace NuGet.VisualStudio
{
    public interface IVsPackageManagerFactory
    {
        IVsPackageManager CreatePackageManager();

        IVsPackageManager CreatePackageManager(IPackageRepository repository, bool useFallbackForDependencies);

        IVsPackageManager CreatePackageManager(IPackageRepository repository, bool useFallbackForDependencies, bool addToRecent);
    }
}