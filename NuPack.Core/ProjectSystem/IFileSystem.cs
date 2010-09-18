namespace NuPack {
    using System.Collections.Generic;
    using System.IO;

    public interface IFileSystem {
        IPackageEventListener Listener { get; set; }
        string Root { get; }
        void DeleteDirectory(string path, bool recursive);
        IEnumerable<string> GetFiles(string path);
        IEnumerable<string> GetFiles(string path, string filter);
        IEnumerable<string> GetDirectories(string path);
        void DeleteFile(string path);
        bool FileExists(string path);
        bool DirectoryExists(string path);
        void AddFile(string path, Stream stream);
        Stream OpenFile(string path);
    }
}