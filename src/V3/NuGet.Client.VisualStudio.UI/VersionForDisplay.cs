using System;
using NuGet.Versioning;

namespace NuGet.Client.VisualStudio.UI
{
    public class VersionForDisplay
    {
        private string _additionalInfo;

        public VersionForDisplay(
            NuGetVersion version,
            string additionalInfo)
        {
            Version = version;
            _additionalInfo = additionalInfo;
        }

        public NuGetVersion Version
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return _additionalInfo + Version.ToNormalizedString();
        }

        public override bool Equals(object obj)
        {
            var other = obj as VersionForDisplay;
            return other != null && other.Version == Version;
        }

        public override int GetHashCode()
        {
            return Version.GetHashCode();
        }
    }
}
