using System;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public static class VsVersionHelper
    {
        private const int MaxVsVersion = 12;
        private static readonly Lazy<int> _vsMajorVersion = new Lazy<int>(GetMajorVsVersion);

        public static int VsMajorVersion
        {
            get { return _vsMajorVersion.Value; }
        }

        public static bool IsVisualStudio2010
        {
            get { return VsMajorVersion == 10; }
        }

        public static bool IsVisualStudio2012
        {
            get { return VsMajorVersion == 11; }
        }

        private static int GetMajorVsVersion()
        {
            DTE dte = ServiceLocator.GetInstance<DTE>();
            string vsVersion = dte.Version;
            Version version;
            if (Version.TryParse(vsVersion, out version))
            {
                return version.Major;
            }
            return MaxVsVersion;
        }
    }
}
