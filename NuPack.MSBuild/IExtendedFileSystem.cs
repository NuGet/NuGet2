using System;
using System.IO;

namespace NuGet {
    public interface IExtendedFileSystem : IFileSystem {
        Stream CreateFile(string path);
        string GetCurrentDirectory();
        string GetFullPath(string path);
    }
}
