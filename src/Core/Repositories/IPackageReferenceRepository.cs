using System.Runtime.Versioning;

namespace NuGet
{
    public interface IPackageReferenceRepository : IPackageRepository
    {
        void AddPackage(string packageId, SemanticVersion version, FrameworkName targetFramework, string source);
        FrameworkName GetPackageTargetFramework(string packageId);
    }
}
