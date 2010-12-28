namespace NuGet.VisualStudio {
    public interface ISourceControlResolver {
        IFileSystem GetFileSystem(string path);
    }
}
