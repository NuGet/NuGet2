using System;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class NugetProgramStatic : IDisposable
    {
        public NugetProgramStatic()
        {
            Program.IgnoreExtensions = true;
        }

        public void Dispose()
        {
            Program.IgnoreExtensions = false;
        }
    }
}