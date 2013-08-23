using System.Globalization;
using System.Resources;
using System.Threading;

namespace NuGet
{
    internal static class LocalizedResourceManager
    {
        private static readonly ResourceManager _resourceManager = new ResourceManager("NuGet.NuGetResources", typeof(LocalizedResourceManager).Assembly);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification="the convention is to used lower case letter for language name.")]
        public static string GetString(string resourceName)
        {
            var culture = Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName.ToLowerInvariant();
            return _resourceManager.GetString(resourceName + '_' + culture, CultureInfo.InvariantCulture) ??
                   _resourceManager.GetString(resourceName, CultureInfo.InvariantCulture);
        }
    }
}
