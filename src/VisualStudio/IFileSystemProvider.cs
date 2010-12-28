namespace NuGet.VisualStudio {
    public interface IFileSystemProvider {
        IFileSystem GetFileSystem(string path);
    }
}
