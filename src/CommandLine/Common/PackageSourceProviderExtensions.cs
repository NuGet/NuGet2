using System;

namespace NuGet.Common
{
    public static class PackageSourceProviderExtensions
    {
        public static string ResolveAndValidateSource(this IPackageSourceProvider sourceProvider, string source)
        {
            if (String.IsNullOrEmpty(source))
            {
                return null;
            }

            source = sourceProvider.ResolveSource(source);
            CommandLineUtility.ValidateSource(source);
            return source;
        }
    }
}
