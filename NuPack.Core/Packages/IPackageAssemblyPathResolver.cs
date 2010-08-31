namespace NuPack {
    public interface IPackageAssemblyPathResolver {
        string GetAssemblyPath(Package package, IPackageAssemblyReference assemblyReference);
    }
}
