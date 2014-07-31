using System;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class NugetProgramStatic : IDisposable
    {
        public NugetProgramStatic()
        {
            Program.IgnoreExtensions = true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
        public void Dispose()
        {
            Program.IgnoreExtensions = false;
        }
    }
}