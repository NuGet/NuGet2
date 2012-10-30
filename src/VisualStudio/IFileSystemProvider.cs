namespace NuGet.VisualStudio
{
    public interface IFileSystemProvider
    {
        IFileSystem GetFileSystem(string path);
        IFileSystem GetFileSystem(string path, bool ignoreSourceControlSetting);
    }
}