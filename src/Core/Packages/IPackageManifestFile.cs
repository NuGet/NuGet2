using System.Collections.Generic;

namespace NuGet
{
    public interface IPackageManifestFile
    {
        string Source { get; }
        string Target { get; }
        string Exclude { get; }

        IEnumerable<IPackageManifestFileProperty> Properties { get; }
    }
}