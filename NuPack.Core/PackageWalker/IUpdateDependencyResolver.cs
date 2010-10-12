namespace NuPack {
    public interface IUpdateDependencyResolver {
        PackagePlan ResolveDependencies(IPackage oldPackage, IPackage newPackage);
    }
}
