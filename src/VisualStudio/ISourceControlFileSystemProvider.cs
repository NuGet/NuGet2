using EnvDTE80;

namespace NuGet.VisualStudio
{
    public interface ISourceControlFileSystemProvider
    {
        ISourceControlFileSystem GetFileSystem(string path, SourceControlBindings binding);
    }
}
