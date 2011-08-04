using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace NuGet {
    public interface IPackage : IPackageMetadata, IServerPackageMetadata {
        bool IsLatestVersion { get; }

        // It's nullable since some package types won't support it (in memory packages)
        DateTimeOffset? Published { get; }

        IEnumerable<IPackageAssemblyReference> AssemblyReferences { get; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        IEnumerable<IPackageFile> GetFiles();

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        Stream GetStream();
    }
}
