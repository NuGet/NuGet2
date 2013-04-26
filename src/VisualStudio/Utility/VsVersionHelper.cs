using System;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public static class VsVersionHelper
    {
        private const int MaxVsVersion = 12;
        private static readonly Lazy<int> _vsMajorVersion = new Lazy<int>(GetMajorVsVersion);
        private static readonly Lazy<string> _fullVsEdition = new Lazy<string>(GetFullVsVersionString);

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

        public static string FullVsEdition
        {
            get { return _fullVsEdition.Value; }
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

        private static string GetFullVsVersionString()
        {
            DTE dte = ServiceLocator.GetInstance<DTE>();

            string edition = dte.Edition;
            if (!edition.StartsWith("VS", StringComparison.OrdinalIgnoreCase))
            {
                edition = "VS " + edition;
            }

            return edition + "/" + dte.Version;
        }
    }
}