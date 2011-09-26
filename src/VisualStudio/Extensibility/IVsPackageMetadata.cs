using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NuGet.VisualStudio {
    [ComImport]
    [Guid("8B3C4B38-632E-436C-8934-4669C6118845")]
    public interface IVsPackageMetadata {
        string Id { get; }
        SemVer Version { get; }
        string Title { get; }
        string Description { get; }
        IEnumerable<string> Authors { get; }
        string InstallPath { get; }
    }
}
