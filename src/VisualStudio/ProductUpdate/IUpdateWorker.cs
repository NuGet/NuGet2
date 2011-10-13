using System;

namespace NuGet.VisualStudio
{
    internal interface IUpdateWorker
    {
        bool CheckForUpdate(out Version installedVersion, out Version newVersion);
    }
}
