using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace NuGet
{
    public static class ResourceHelper
    {
        private static Dictionary<Type, ResourceManager> _cachedManagers;

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

            if (_cachedManagers == null)
            {
                _cachedManagers = new Dictionary<Type, ResourceManager>();
            }

            ResourceManager resourceManager;
            if (!_cachedManagers.TryGetValue(resourceType, out resourceManager))
            {
                PropertyInfo property = resourceType.GetProperty("ResourceManager", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

                if (property == null || property.GetGetMethod(nonPublic: true) == null)
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture, NuGetResources.ResourceTypeDoesNotHaveProperty, resourceType, "ResourceManager"));
                }

                if (property.PropertyType != typeof(ResourceManager))
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture, NuGetResources.ResourcePropertyIncorrectType, resourceName, resourceType));
                }

                resourceManager = (ResourceManager)property.GetGetMethod(nonPublic: true)
                                                           .Invoke(obj: null, parameters: null);
            }
            string value = (string)resourceManager.GetString(resourceName);
            if (String.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture, NuGetResources.ResourceTypeDoesNotHaveProperty, resourceType, resourceName));
            }

            return value;
        }
    }
}
