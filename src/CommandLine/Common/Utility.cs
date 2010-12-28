namespace NuGet {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Reflection;
    using Microsoft.Internal.Web.Utils;
    using NuGet.Common;

    public static class CommandLineUtility {
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

        private static Dictionary<string, string> _cachedResourceStrings;
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
    }
}
