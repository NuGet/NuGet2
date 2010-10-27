using System;
using System.Collections.Generic;

namespace NuGet {
    public interface IPackageMetadata {
        string Id { get; }
        Version Version { get; }
        string Title { get; }
        IEnumerable<string> Authors { get; }       
        Uri IconUrl { get; }
        Uri LicenseUrl { get; }
        Uri ProjectUrl { get; }
        bool RequireLicenseAcceptance { get; }
        string Description { get; }
        string Summary { get; }
        string Language { get; }
        IEnumerable<PackageDependency> Dependencies { get; }
    }
}
