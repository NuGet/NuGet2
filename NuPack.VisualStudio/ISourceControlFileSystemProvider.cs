using EnvDTE80;
using EnvDTE;

namespace NuPack.VisualStudio {
    public interface ISourceControlFileSystemProvider {
        IFileSystem GetFileSystem(string path, SourceControlBindings binding);
    }
}