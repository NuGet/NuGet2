
namespace NuGet
{
    public interface IPackageLookup
    {
        IPackage FindPackage(string packageId, SemanticVersion version);
    }
}
