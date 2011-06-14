using System;

namespace NuGet.Common {
    public static class PackageSourceProviderExtensions {
        public static string ResolveAndValidateSource(this IPackageSourceProvider sourceProvier, string source) {
            if (String.IsNullOrEmpty(source)) {
                return null;
            }

            source = sourceProvier.ResolveSource(source);
            CommandLineUtility.ValidateSource(source);
            return source;
        }

        public static string GetDisplayName(this IPackageSourceProvider sourceProvider, string source) {
            if (String.IsNullOrEmpty(source) || source.Equals(NuGetConstants.DefaultGalleryServerUrl)) {
                return NuGetResources.LiveFeed + " (" + NuGetConstants.DefaultGalleryServerUrl + ")";
            }
            if (source.Equals(NuGetConstants.DefaultSymbolServerUrl)) {
                return NuGetResources.DefaultSymbolServer + " (" + NuGetConstants.DefaultSymbolServerUrl + ")";
            }
            return "'" + source + "'";
        }
    }
}
