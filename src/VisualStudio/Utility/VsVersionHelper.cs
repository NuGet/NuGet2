using System;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public static class VsVersionHelper
    {
        private const int MaxVsVersion = 11;
        private static readonly Lazy<int> _vsMajorVersion = new Lazy<int>(GetMajorVsVersion);

        public static int VsMajorVersion
        {
            get { return _vsMajorVersion.Value; }
        }

        public static bool IsVisualStudio2010
        {
            get { return VsMajorVersion == 10; }
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



        public static string GetSKU()
        {
            DTE dte = ServiceLocator.GetInstance<DTE>();
            string sku = dte.Edition;
            if (sku.Equals("Ultimate", StringComparison.OrdinalIgnoreCase) || 
                sku.Equals("Premium", StringComparison.OrdinalIgnoreCase) || 
                sku.Equals("Professional", StringComparison.OrdinalIgnoreCase))
            {
                sku = "Pro";
            }

            return sku;

        }
    }
}