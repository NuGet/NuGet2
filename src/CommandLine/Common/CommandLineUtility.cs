using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet {
    internal static class CommandLineUtility {
        public readonly static string ApiKeysSectionName = "apikeys";
        
        public static string GetApiKey(ISettings settings, string source, bool throwIfNotFound = true) {
            var value = settings.GetDecryptedValue(CommandLineUtility.ApiKeysSectionName, source);
            if (String.IsNullOrEmpty(value) && throwIfNotFound) {
                throw new CommandLineException(NuGetResources.NoApiKeyFound, GetSourceDisplayName(source));
            }
            return value;
        }

        public static string GetSourceDisplayName(string source) {
            if (String.IsNullOrEmpty(source) || source.Equals(GalleryServer.DefaultGalleryServerUrl)) {
                return NuGetResources.LiveFeed + " (" + GalleryServer.DefaultGalleryServerUrl + ")";
            }
            if (source.Equals(GalleryServer.DefaultSymbolServerUrl)) {
                return NuGetResources.DefaultSymbolServer + " (" + GalleryServer.DefaultSymbolServerUrl + ")";
            }
            return "'" + source + "'";
        }

        public static void ValidateSource(string source) {
            if (!PathValidator.IsValidUrl(source)) {
                throw new CommandLineException(NuGetResources.InvalidSource, source);
            }
        }

        public static string GetUnambiguousFile(string searchPattern) {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), searchPattern);
            if (files.Length == 1) {
                return files[0];
            }

            return null;
        }

#if DEBUG
        internal static void WaitForDebugger() {
            System.Console.WriteLine("Waiting for debugger");
            while (!System.Diagnostics.Debugger.IsAttached) {

            }
        }
#endif
    }
}
