
namespace NuGet.VisualStudio {
    public interface IVsPackageRepositoryFactory : IPackageRepositoryFactory {
        IPackageRepository CreateRepository(string source);
    }
}
