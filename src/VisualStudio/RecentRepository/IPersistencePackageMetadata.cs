using System;

namespace NuGet.VisualStudio {
    public interface IPersistencePackageMetadata {
        string Id { get; }
        SemanticVersion Version { get; }
        DateTime LastUsedDate { get; }
    }
}