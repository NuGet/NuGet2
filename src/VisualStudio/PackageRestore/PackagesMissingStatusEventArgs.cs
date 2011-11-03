using System;

namespace NuGet.VisualStudio
{
    public class PackagesMissingStatusEventArgs : EventArgs
    {
        public bool PackagesMissing { get; private set; }

        public PackagesMissingStatusEventArgs(bool packagesMissing)
        {
            PackagesMissing = packagesMissing;
        }
    }
}