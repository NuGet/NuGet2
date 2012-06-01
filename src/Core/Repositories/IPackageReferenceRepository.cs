using System.Runtime.Versioning;

namespace NuGet
{
    public interface IPackageReferenceRepository : IPackageRepository
    {
        void AddPackage(string packageId, SemanticVersion version, FrameworkName targetFramework);
        FrameworkName GetPackageTargetFramework(string packageId);
    }
}
