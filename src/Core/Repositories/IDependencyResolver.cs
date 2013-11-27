namespace NuGet
{
    public interface IDependencyResolver
    {
        IPackage ResolveDependency(PackageDependency dependency, IPackageConstraintProvider constraintProvider, bool allowPrereleaseVersions, bool preferListedPackages, DependencyVersion dependencyVersion);
    }
}
