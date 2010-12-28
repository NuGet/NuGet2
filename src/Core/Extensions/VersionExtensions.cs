using System;

namespace NuGet {
    public static class VersionExtensions {
        public static Func<IPackage, bool> ToDelegate(this IVersionSpec versionInfo) {
            return versionInfo.ToDelegate<IPackage>(p => p.Version);
        }

        public static Func<T, bool> ToDelegate<T>(this IVersionSpec versionInfo, Func<T, Version> extractor) {
            return p => {
                Version version = extractor(p);
                bool condition = true;
                if (versionInfo.MinVersion != null) {
                    if (versionInfo.IsMinInclusive) {
                        condition = condition && version >= versionInfo.MinVersion;
                    }
                    else {
                        condition = condition && version > versionInfo.MinVersion;
                    }
                }

                if (versionInfo.MaxVersion != null) {
                    if (versionInfo.IsMaxInclusive) {
                        condition = condition && version <= versionInfo.MaxVersion;
                    }
                    else {
                        condition = condition && version < versionInfo.MaxVersion;
                    }
                }

                return condition;
            };
        }
    }
}
