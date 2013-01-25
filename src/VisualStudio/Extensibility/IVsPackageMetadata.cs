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
        string VersionString { get;  }
        string Title { get; }
        string Description { get; }
        IEnumerable<string> Authors { get; }
        string InstallPath { get; }
    }
}