using EnvDTE80;
using EnvDTE;

namespace NuGet.VisualStudio {
    public interface ISourceControlFileSystemProvider {
        IFileSystem GetFileSystem(string path, SourceControlBindings binding);
    }
}
