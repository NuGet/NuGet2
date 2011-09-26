using System;
using System.Collections.Generic;

namespace NuGet {
    public interface IPackageMetadata {
        string Id { get; }
        SemVer Version { get; }
        string Title { get; }
        IEnumerable<string> Authors { get; }
        IEnumerable<string> Owners { get; }
        Uri IconUrl { get; }
        Uri LicenseUrl { get; }
        Uri ProjectUrl { get; }
        bool RequireLicenseAcceptance { get; }
        string Description { get; }
        string Summary { get; }
        string ReleaseNotes { get; }
        string Language { get; }
        string Tags { get; }
        string Copyright { get; }

        /// <summary>
        /// Specifies assemblies from GAC that the package depends on.
        /// </summary>
        IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies { get; }
        
        /// <summary>
        /// Specifies other packages that the package depends on.
        /// </summary>
        IEnumerable<PackageDependency> Dependencies { get; }
    }
}
