using System;

namespace NuPack.Server.Infrastructure {
    public interface IPackageStore {
        DateTimeOffset GetLastModified(string packageFileName);

        string GetFullPath(string path);
    }
}