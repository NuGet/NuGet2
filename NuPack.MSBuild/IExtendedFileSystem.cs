using System;
using System.IO;

namespace NuGet {
    public interface IExtendedFileSystem : IFileSystem {
        Stream CreateFile(string path);
        string GetCurrentDirectory();
    }
}
