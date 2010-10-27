namespace NuPack.VisualStudio {
    public interface IVsPackageManagerFactory {
        IVsPackageManager CreatePackageManager();
        IVsPackageManager CreatePackageManager(string source);
    }
}
