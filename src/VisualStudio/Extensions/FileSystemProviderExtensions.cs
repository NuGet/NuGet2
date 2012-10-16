namespace NuGet.VisualStudio
{
    public static class FileSystemProviderExtensions
    {
        public static IFileSystem GetFileSystem(this IFileSystemProvider provider, string path)
        {
            return provider.GetFileSystem(path, ignoreSourceControlSetting: false);
        }
    }
}
