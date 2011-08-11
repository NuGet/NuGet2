using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace NuGet {
    internal class ManifestVersionUtility {
        private static readonly Type[] _xmlAttributes = new[] { typeof(XmlElementAttribute), typeof(XmlAttributeAttribute), typeof(XmlArrayAttribute) };

        private const int DefaultVersion = 1;
        public static int GetManifestVersion(ManifestMetadata metadata) {
            return VisitObject(metadata);
        }

        private static int VisitObject(object o) {
            return (from item in o.GetType().GetProperties()
                    select VisitProperty(o, item)).Max();
        }

        public static int VisitProperty(object o, PropertyInfo property) {
            if (!IsManifestMetadata(property)) {
                return DefaultVersion;
            }

            var value = property.GetValue(o, index: null);
            if (value == null) {
                return DefaultVersion;
            }

            int version = GetPropertyVersion(property);

            if (typeof(IList).IsAssignableFrom(property.PropertyType)) {
                var list = (IList)value;
                if (list != null && list.Count > 0) {
                    return Math.Max(version, VisitList(list));
                }
                return DefaultVersion;
            }

            if (property.PropertyType == typeof(string)) {
                var stringValue = (string)value;
                if (!String.IsNullOrEmpty(stringValue)) {
                    return version;
                }
                return DefaultVersion;
            }

            // For all other object types a null check would suffice.
            return version;
        }

        private static int VisitList(IList list) {
            int version = DefaultVersion;

            foreach (var item in list) {
                version = Math.Max(version, VisitObject(item));
            }

            return version;
        }

        private static int GetPropertyVersion(PropertyInfo property) {
            var attribute = GetAttribute<ManifestVersionAttribute>(property);
            return attribute != null ? attribute.Version : DefaultVersion;
        }

        private static bool IsManifestMetadata(PropertyInfo property) {
            return _xmlAttributes.Any(c => GetAttribute(property, c) != null);
        }

        private static T GetAttribute<T>(ICustomAttributeProvider attributeProvider) {
            return (T)GetAttribute(attributeProvider, typeof(T));
        }

        private static object GetAttribute(ICustomAttributeProvider attributeProvider, Type type) {
            return attributeProvider.GetCustomAttributes(type, inherit: false)
                                       .FirstOrDefault();
        }
    }
}
