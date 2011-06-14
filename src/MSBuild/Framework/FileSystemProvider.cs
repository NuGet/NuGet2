using System.Collections.Generic;

namespace NuGet.MSBuild {
    public class FileSystemProvider : IFileSystemProvider {

        public IFileSystem CreateFileSystem(string root) {
            return new PhysicalFileSystem(root);
        }
    }
}