namespace NuGet
{
    public interface ILatestPackageLookup
    {
        bool TryFindLatestPackageById(string id, out SemanticVersion latestVersion);
    }
}