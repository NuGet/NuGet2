namespace NuPack {
    using System;
    using System.Collections.Generic;

    public interface IPackage {
        string Id { get; }
        string Category { get; }
        string Description { get; }
        string Language { get; }
        string LastModifiedBy { get; }
        IEnumerable<string> Keywords { get; }
        IEnumerable<string> Authors { get; }
        DateTime Created { get; }
        DateTime Modified { get; }
        Version Version { get; }
        IEnumerable<PackageDependency> Dependencies { get; }
        IEnumerable<IPackageAssemblyReference> AssemblyReferences { get; }
        IEnumerable<IPackageFile> GetFiles();
    }
}
