namespace NuPack {
    using System;
    using System.Collections.Generic;

    public interface IPackage {
        string Id { get; }
        string Category { get; }
        string Description { get; }
        string Language { get; }
        string LastModifiedBy { get; }
        bool RequireLicenseAcceptance { get; }
        Uri LicenseUrl { get; }
        IEnumerable<string> Keywords { get; }
        IEnumerable<string> Authors { get; }
        DateTime Created { get; }
        DateTime Modified { get; }
        Version Version { get; }
        IEnumerable<PackageDependency> Dependencies { get; }
        IEnumerable<IPackageAssemblyReference> AssemblyReferences { get; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design", 
            "CA1024:UsePropertiesWhereAppropriate",
            Justification="This method is potentially expensive.")]
        IEnumerable<IPackageFile> GetFiles();
    }
}
