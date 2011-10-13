using System;
using System.IO;

namespace NuGet
{
    internal static class CommandLineUtility
    {
        public readonly static string ApiKeysSectionName = "apikeys";

        public static string GetApiKey(ISettings settings, string source, bool throwIfNotFound = true)
        {
            var value = settings.GetDecryptedValue(CommandLineUtility.ApiKeysSectionName, source);
            if (String.IsNullOrEmpty(value) && throwIfNotFound)
            {
                throw new CommandLineException(NuGetResources.NoApiKeyFound, GetSourceDisplayName(source));
            }
            return value;
        }

        public static void ValidateSource(string source)
        {
            if (!PathValidator.IsValidUrl(source))
            {
                throw new CommandLineException(NuGetResources.InvalidSource, source);
            }
        }

        public static string GetSourceDisplayName(string source)
        {
            if (String.IsNullOrEmpty(source) || source.Equals(NuGetConstants.DefaultGalleryServerUrl, StringComparison.OrdinalIgnoreCase))
            {
                return NuGetResources.LiveFeed + " (" + NuGetConstants.DefaultGalleryServerUrl + ")";
            }
            if (source.Equals(NuGetConstants.DefaultSymbolServerUrl, StringComparison.OrdinalIgnoreCase))
            {
                return NuGetResources.DefaultSymbolServer + " (" + NuGetConstants.DefaultSymbolServerUrl + ")";
            }
            return "'" + source + "'";
        }

        public static string GetUnambiguousFile(string searchPattern)
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), searchPattern);
            if (files.Length == 1)
            {
                return files[0];
            }

            return null;
        }
    }
}