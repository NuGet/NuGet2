using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Internal.Web.Utils;
using NuGet.Common;
using NuGet.Commands;
using System.IO;

namespace NuGet {
    internal static class CommandLineUtility {
        private static Dictionary<string, string> _cachedResourceStrings;

        private static readonly HashSet<string> _supportedProjectExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {  
            ".csproj",
            ".vbproj",
            ".fsproj",
        };

        public readonly static string ApiKeysSectionName = "apikeys";

        public static Type RemoveNullableFromType(Type type) {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        public static object ChangeType(object value, Type type) {
            if (type == null) {
                throw new ArgumentNullException("type");
            }

            if (value == null) {
                if (TypeAllowsNull(type)) {
                    return null;
                }
                return Convert.ChangeType(value, type, CultureInfo.CurrentCulture);
            }

            type = RemoveNullableFromType(type);

            if (value.GetType() == type) {
                return value;
            }

            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(value.GetType())) {
                return converter.ConvertFrom(value);
            }

            TypeConverter otherConverter = TypeDescriptor.GetConverter(value.GetType());
            if (otherConverter.CanConvertTo(type)) {
                return otherConverter.ConvertTo(value, type);
            }

            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                NuGetResources.UnableToConvertTypeError, value.GetType(), type));
        }

        public static bool TypeAllowsNull(Type type) {
            return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
        }

        public static Type GetGenericCollectionType(Type type) {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>)) {
                return type;
            }
            return (from t in type.GetInterfaces()
                    where t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>)
                    select t).SingleOrDefault();
        }

        public static bool IsMultiValuedProperty(PropertyInfo property) {
            return GetGenericCollectionType(property.PropertyType) != null;
        }

        public static string GetLocalizedString(Type resourceType, string resourceName) {
            if (String.IsNullOrEmpty(resourceName)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "resourceName");
            }

            if (resourceType == null) {
                throw new ArgumentNullException("resourceType");
            }

            if (_cachedResourceStrings == null) {
                _cachedResourceStrings = new Dictionary<string, string>();
            }

            if (!_cachedResourceStrings.ContainsKey(resourceName)) {
                PropertyInfo property = resourceType.GetProperty(resourceName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

                if (property == null) {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture, NuGetResources.ResourceTypeDoesNotHaveProperty, resourceType, resourceName));
                }

                if (property.PropertyType != typeof(string)) {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture, NuGetResources.ResourcePropertyNotStringType, resourceName, resourceType));
                }

                MethodInfo getMethod = property.GetGetMethod(true);
                if ((getMethod == null) || (!getMethod.IsAssembly && !getMethod.IsPublic)) {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture, NuGetResources.ResourcePropertyDoesNotHaveAccessibleGet, resourceType, resourceName));
                }

                _cachedResourceStrings[resourceName] = (string)property.GetValue(null, null);
            }

            return _cachedResourceStrings[resourceName];
        }

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

        public static bool TryGetProjectFile(out string projectFile) {
            projectFile = null;
            var files = Directory.GetFiles(Directory.GetCurrentDirectory());

            var candidates = files.Where(file => _supportedProjectExtensions.Contains(Path.GetExtension(file)))
                                  .ToList();

            switch (candidates.Count) {
                case 1:
                    projectFile = candidates.Single();
                    break;
            }

            return !String.IsNullOrEmpty(projectFile);
        }
    }
}
