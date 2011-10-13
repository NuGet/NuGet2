
namespace NuGet.MSBuild
{
    public interface IFileSystemProvider
    {
        IFileSystem CreateFileSystem(string root);
    }
}
