namespace NuPack {
    public interface IPackageRepositoryFactory {
        IPackageRepository CreateRepository(string source);
    }
}
