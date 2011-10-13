using System;

namespace NuGet.VisualStudio
{
    internal class NullUpdateWorker : IUpdateWorker
    {
        public bool CheckForUpdate(out Version installedVersion, out Version newVersion)
        {
            installedVersion = newVersion = null;
            return false;
        }
    }
}
