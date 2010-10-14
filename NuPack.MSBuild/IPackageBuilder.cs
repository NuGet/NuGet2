using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace NuPack.Authoring {
    public interface IPackageBuilder {
        Collection<string> Authors { get; }
        string Category { get; set; }
        DateTime Created { get; set; }
        Collection<PackageDependency> Dependencies { get; }
        string Description { get; set; }
        Collection<IPackageFile> Files { get; }
        string Id { get; set; }
        Collection<string> Keywords { get; }
        string Language { get; set; }
        string LastModifiedBy { get; set; }
        Uri LicenseUrl { get; set; }
        DateTime Modified { get; set; }
        bool RequireLicenseAcceptance { get; set; }
        void Save(Stream stream);
        Version Version { get; set; }
    }
}
