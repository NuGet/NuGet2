namespace NuGet.VisualStudio {
    public interface IVsPackageManagerFactory {
        IVsPackageManager CreatePackageManager();
        IVsPackageManager CreatePackageManager(string source);
    }
}
