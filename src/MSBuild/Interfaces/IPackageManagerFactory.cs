namespace NuGet.MSBuild {
    public interface IPackageManagerFactory {
        IPackageManager CreateFrom(IPackageRepository packageRepository, string path);
    }
}