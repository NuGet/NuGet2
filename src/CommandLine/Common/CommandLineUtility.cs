using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NuGet
{
    public static class CommandLineUtility
    {
        public readonly static string ApiKeysSectionName = "apikeys";

        public static string GetApiKey(ISettings settings, string source)
        {
            var value = settings.GetDecryptedValue(CommandLineUtility.ApiKeysSectionName, source);
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

        public static ICollection<PackageReference> GetPackageReferences(PackageReferenceFile file, string fileName, bool requireVersion)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            var packageReferences = file.GetPackageReferences(requireVersion).ToList();
            foreach (var package in packageReferences)
            {
                // GetPackageReferences returns all records without validating values. We'll throw if we encounter packages
                // with malformed ids / Versions.
                if (String.IsNullOrEmpty(package.Id))
                {
                    throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture, NuGetResources.InstallCommandInvalidPackageReference, fileName));
                }
                if (requireVersion && (package.Version == null))
                {
                    throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture, NuGetResources.InstallCommandPackageReferenceInvalidVersion, package.Id));
                }
            }

            return packageReferences;
        }
    }
}