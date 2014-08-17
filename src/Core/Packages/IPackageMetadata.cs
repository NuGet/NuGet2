using System;
using System.Collections.Generic;

namespace NuGet
{
    public interface IPackageMetadata : IPackageName
    {
        string Title { get; }
        IEnumerable<string> Authors { get; }
        IEnumerable<string> Owners { get; }
        Uri IconUrl { get; }
        Uri LicenseUrl { get; }
        Uri ProjectUrl { get; }
        bool RequireLicenseAcceptance { get; }
        bool DevelopmentDependency { get; }
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
        /// Returns sets of References specified in the manifest.
        /// </summary>
        ICollection<PackageReferenceSet> PackageAssemblyReferences { get; }

        /// <summary>
        /// Specifies sets other packages that the package depends on.
        /// </summary>
        IEnumerable<PackageDependencySet> DependencySets { get; }

        /// <summary>
        /// The minimum NuGet client version required to consume this package
        /// </summary>
        Version MinClientVersion { get; }

        /// <summary>
        /// The source code repository URL for the package. Meant for tooling.
        /// </summary>
        Uri RepositoryUrl { get; }

        /// <summary>
        /// The source code repository type (such as 'git') for the package. Meant for tooling.
        /// </summary>
        string RepositoryType { get; }

        /// <summary>
        /// The author-specified friendly license names.
        /// </summary>
        string LicenseNames { get; }

        /// <summary>
        /// Arbitrary properties for the package. Future NuGet client versions may begin to
        /// understand specific properties within this list, but custom properties can flow
        /// through as well.
        /// </summary>
        /// <remarks>
        /// Future NuGet clients will only understand properties within
        /// this list if the package specifies a MinClientVersion for the version that adds
        /// understanding of the property.
        /// </remarks>
        IEnumerable<PackageProperty> Properties { get; }
    }
}