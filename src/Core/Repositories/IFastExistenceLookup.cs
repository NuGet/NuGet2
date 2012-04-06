
namespace NuGet
{
    public interface IFastExistenceLookup
    {
        bool Exists(string packageId, SemanticVersion version);
    }
}