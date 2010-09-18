namespace NuPack {
    public interface IPackageAssemblyPathResolver {
        string GetAssemblyPath(IPackage package, IPackageAssemblyReference assemblyReference);
    }
}
