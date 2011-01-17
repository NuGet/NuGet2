using System;

namespace NuGet.VisualStudio {

    /// <summary>
    /// Defines the fields that will be persisted in order to reconstruct a package from a source.
    /// Used for the Recent packages feature.
    /// </summary>
    public interface IPersistencePackageMetadata {
        string Id { get; }
        Version Version { get;  }
        string Source { get; }
    }
}