using System;

namespace NuGet.Server.Infrastructure {
    public interface IPackageStore {
        DateTimeOffset GetLastModified(string packageFileName);

        string GetFullPath(string path);
    }
}
