using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("NuGet.Dialog10")]
[assembly: AssemblyDescription("Manage NuGet Package dialog for Visual Studio 2010")]
[assembly: CLSCompliant(false)]

[assembly: InternalsVisibleTo("NuGet.TestUI")]
[assembly: InternalsVisibleTo("NuGet.Dialog.Test")]

// dynamic assembly used by Moq to host proxies
#pragma warning disable 1700
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#pragma warning restore 1700