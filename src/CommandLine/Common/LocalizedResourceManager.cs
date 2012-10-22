using System.Resources;
using System.Threading;

namespace NuGet
{
    internal static class LocalizedResourceManager
    {
        private static readonly ResourceManager _resourceManager = new ResourceManager("NuGet.NuGetResources", typeof(LocalizedResourceManager).Assembly);

        public static string GetString(string resourceName)
        {
            var culture = Thread.CurrentThread.CurrentUICulture.ThreeLetterISOLanguageName;
            return _resourceManager.GetString(resourceName + '_' + culture) ??
                   _resourceManager.GetString(resourceName);
        }
    }
}
