using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NuGet.VisualStudio
{
    [ComImport]
    [Guid("8B3C4B38-632E-436C-8934-4669C6118845")]
    public interface IVsPackageMetadata
    {
        string Id { get; }
        [Obsolete("Do not use this property because it will require referencing NuGet.Core.dll assembly. Use the VersionString property instead.")]
        SemanticVersion Version { get; }
        string Title { get; }
        string Description { get; }
        IEnumerable<string> Authors { get; }
        string InstallPath { get; }

        // IMPORTANT: This property must come LAST, because it was added in 2.5. Having it declared 
        // LAST will avoid breaking components that compiled against earlier versions which doesn't
        // have this property.
        string VersionString { get; }
    }
}