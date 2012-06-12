using System.Globalization;
using System.Resources;

namespace NuGet
{
    internal static class LocalizedResourceManager
    {
        private static readonly ResourceManager _resourceManager = new ResourceManager("NuGet.NuGetResources", typeof(LocalizedResourceManager).Assembly);
        private static readonly string _currentCulture = CultureInfo.CurrentCulture.Name;

        public static string GetString(string resourceName)
        {
            return _resourceManager.GetString(resourceName + '.' + _currentCulture) ??
                   _resourceManager.GetString(resourceName);
        }
    }
}
