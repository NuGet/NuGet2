using System;

namespace NuGet {
    public static class VersionExtensions {
        public static Func<IPackage, bool> ToDelegate(this IVersionSpec versionInfo) {
            return versionInfo.ToDelegate<IPackage>(p => p.Version);
        }

        public static Func<T, bool> ToDelegate<T>(this IVersionSpec versionInfo, Func<T, Version> extractor) {
            return p => {
                Version version = VersionUtility.NormalizeVersion(extractor(p));
                bool condition = true;
                if (versionInfo.MinVersion != null) {
                    if (versionInfo.IsMinInclusive) {
                        condition = condition && version >= VersionUtility.NormalizeVersion(versionInfo.MinVersion);
                    }
                    else {
                        condition = condition && version > VersionUtility.NormalizeVersion(versionInfo.MinVersion);
                    }
                }

                if (versionInfo.MaxVersion != null) {
                    if (versionInfo.IsMaxInclusive) {
                        condition = condition && version <= VersionUtility.NormalizeVersion(versionInfo.MaxVersion);
                    }
                    else {
                        condition = condition && version < VersionUtility.NormalizeVersion(versionInfo.MaxVersion);
                    }
                }

                return condition;
            };
        }

        /// <summary>
        /// Determines if the specified version is within the version spec
        /// </summary>
        public static bool Satisfies(this IVersionSpec versionSpec, Version version) {
            // The range is unbounded so return true
            if (versionSpec == null) {
                return true;
            }
            return versionSpec.ToDelegate<Version>(v => v)(version);
        }
    }
}
