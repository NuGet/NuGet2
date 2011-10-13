namespace NuGet.MSBuild
{
    public class PackageManagerFactory : IPackageManagerFactory
    {
        public IPackageManager CreateFrom(IPackageRepository packageRepository, string path)
        {
            return new PackageManager(packageRepository, path);
        }
    }
}