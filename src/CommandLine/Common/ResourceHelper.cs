using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace NuGet
{
    public static class ResourceHelper
    {
        private static Dictionary<Tuple<Type, string>, string> _cachedResourceStrings;

        public static string GetLocalizedString(Type resourceType, string resourceName)
        {
            if (String.IsNullOrEmpty(resourceName))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "resourceName");
            }

            if (resourceType == null)
            {
                throw new ArgumentNullException("resourceType");
            }

            if (_cachedResourceStrings == null)
            {
                _cachedResourceStrings = new Dictionary<Tuple<Type, string>, string>();
            }

            var key = Tuple.Create(resourceType, resourceName);
            string resourceValue;

            if (!_cachedResourceStrings.TryGetValue(key, out resourceValue))
            {
                StringBuilder sb = new StringBuilder();
                foreach (string name in resourceName.Split(';'))
                {
                    PropertyInfo property = resourceType.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

                    if (property == null)
                    {
                        throw new InvalidOperationException(
                            String.Format(CultureInfo.CurrentCulture, NuGetResources.ResourceTypeDoesNotHaveProperty, resourceType, name));
                    }

                    if (property.PropertyType != typeof(string))
                    {
                        throw new InvalidOperationException(
                            String.Format(CultureInfo.CurrentCulture, NuGetResources.ResourcePropertyNotStringType, name, resourceType));
                    }

                    MethodInfo getMethod = property.GetGetMethod(true);
                    if ((getMethod == null) || (!getMethod.IsAssembly && !getMethod.IsPublic))
                    {
                        throw new InvalidOperationException(
                            String.Format(CultureInfo.CurrentCulture, NuGetResources.ResourcePropertyDoesNotHaveAccessibleGet, resourceType, name));
                    }
                    sb.Append((string)property.GetValue(null, null));
                }
                resourceValue = sb.ToString();
                _cachedResourceStrings[key] = resourceValue;
            }

            return resourceValue;
        }
    }
}
