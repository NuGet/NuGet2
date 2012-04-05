
namespace NuGet
{
    public interface IPackageLookup
    {
        IPackage FindPackage(string packageId, SemanticVersion version);

        bool Exists(string packageId, SemanticVersion version);
    }
}
