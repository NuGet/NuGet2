using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

[assembly: AssemblyTitle("NuGet.Core")]
[assembly: AssemblyDescription("NuGet.Core is the core framework assembly for NuGet that the rest of NuGet builds upon.")]
[assembly: CLSCompliant(true)]

[assembly: InternalsVisibleTo("NuGet.Test")]
[assembly: InternalsVisibleTo("NuGet.Test.Utility")]
[assembly: InternalsVisibleTo("NuGet.VisualStudio.Test")]
[assembly: InternalsVisibleTo("NuGet.Cmdlets.Test")]

#if !CODE_COVERAGE
[assembly: SecurityTransparent]
#endif
