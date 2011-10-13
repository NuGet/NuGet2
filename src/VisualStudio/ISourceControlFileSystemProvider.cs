using EnvDTE80;

namespace NuGet.VisualStudio
{
    public interface ISourceControlFileSystemProvider
    {
        IFileSystem GetFileSystem(string path, SourceControlBindings binding);
    }
}
