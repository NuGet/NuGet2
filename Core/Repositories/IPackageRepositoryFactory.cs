namespace NuGet {
    public interface IPackageRepositoryFactory {
        IPackageRepository CreateRepository(PackageSource packageSource);
    }
}
