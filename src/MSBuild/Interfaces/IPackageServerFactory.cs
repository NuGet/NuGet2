
namespace NuGet.MSBuild {
    public interface IPackageServerFactory {
        IPackageServer CreateFrom(string source);
    }
}