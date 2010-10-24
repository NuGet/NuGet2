namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    public interface IPackage : IPackageMetadata  {        
        IEnumerable<IPackageAssemblyReference> AssemblyReferences { get; }

        [SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This method is potentially expensive.")]
        IEnumerable<IPackageFile> GetFiles();
        Stream GetStream();
    }
}
