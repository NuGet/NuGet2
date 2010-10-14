using System;
using System.IO;

namespace NuPack {
    public interface IExtendedFileSystem : IFileSystem {
        Stream CreateFile(string path);
        string GetCurrentDirectory();
        string GetFullPath(string path);
    }
}
