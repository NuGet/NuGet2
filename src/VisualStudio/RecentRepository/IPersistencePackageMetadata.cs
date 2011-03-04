using System;

namespace NuGet.VisualStudio {
    public interface IPersistencePackageMetadata {
        string Id { get; }
        Version Version { get; }
        DateTime LastUsedDate { get; }
    }
}