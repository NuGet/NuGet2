using System.Resources;

namespace NuGet
{
    internal static class NuGetResources
    {
        public static ResourceManager ResourceManager = new ResourceManager("NuGet.NuGetResources", typeof(NuGetResources).Assembly);
    }
}
