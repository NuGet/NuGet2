namespace NuGet.VisualStudio {
    public interface IVsPackageManagerFactory {
        IVsPackageManager CreatePackageManager();
        // REVIEW: Should be changed to PackageSource?
        IVsPackageManager CreatePackageManager(string source);

        IVsPackageManager CreatePackageManager(IPackageRepository repository);
    }
}
