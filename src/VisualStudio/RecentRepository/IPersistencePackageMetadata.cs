using System;

namespace NuGet.VisualStudio {
    public interface IPersistencePackageMetadata {
        string Id { get; }
        SemVer Version { get; }
        DateTime LastUsedDate { get; }
    }
}