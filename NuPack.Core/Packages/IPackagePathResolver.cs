namespace NuPack {
    public interface IPackagePathResolver {
        string GetInstallPath(IPackage package);
        string GetPackageDirectory(IPackage package);
        string GetPackageFileName(IPackage package);        
    }
}
