using System;

namespace NuGet.VisualStudio
{
    public class ProductUpdateAvailableEventArgs : EventArgs
    {
        internal ProductUpdateAvailableEventArgs(Version currentVersion, Version newVersion)
        {
            CurrentVersion = currentVersion;
            NewVersion = newVersion;
        }

        public Version CurrentVersion { get; private set; }
        public Version NewVersion { get; private set; }
    }
}