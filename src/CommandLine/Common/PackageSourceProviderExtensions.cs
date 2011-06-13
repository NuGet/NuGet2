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
            if (String.IsNullOrEmpty(source) || source.Equals(GalleryServer.DefaultGalleryServerUrl)) {
                return NuGetResources.LiveFeed + " (" + GalleryServer.DefaultGalleryServerUrl + ")";
            }
            if (source.Equals(GalleryServer.DefaultSymbolServerUrl)) {
                return NuGetResources.DefaultSymbolServer + " (" + GalleryServer.DefaultSymbolServerUrl + ")";
            }
            return "'" + source + "'";
        }
    }
}
