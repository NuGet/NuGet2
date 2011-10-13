
namespace NuGet.MSBuild
{
    public class PackageServerFactory : IPackageServerFactory
    {
        public IPackageServer CreateFrom(string source)
        {
            return new PackageServer(source, MsBuildConstants.UserAgent);
        }
    }
}