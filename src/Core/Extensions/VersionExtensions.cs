using System;
using System.Collections.Generic;
using System.Globalization;

namespace NuGet
{
    public static class VersionExtensions
    {
        public static Func<IPackage, bool> ToDelegate(this IVersionSpec versionInfo)
        {
            if (versionInfo == null)
            {
                throw new ArgumentNullException("versionInfo");
            }
            return versionInfo.ToDelegate<IPackage>(p => p.Version);
        }

        public static Func<T, bool> ToDelegate<T>(this IVersionSpec versionInfo, Func<T, SemanticVersion> extractor)
        {
            if (versionInfo == null)
            {
                throw new ArgumentNullException("versionInfo");
            }
            if (extractor == null)
            {
                throw new ArgumentNullException("extractor");
            }

            return p =>
            {
                SemanticVersion version = extractor(p);
                bool condition = true;
                if (versionInfo.MinVersion != null)
                {
                    if (versionInfo.IsMinInclusive)
                    {
                        condition = condition && version >= versionInfo.MinVersion;
                    }
                    else
                    {
                        condition = condition && version > versionInfo.MinVersion;
                    }
                }

                if (versionInfo.MaxVersion != null)
                {
                    if (versionInfo.IsMaxInclusive)
                    {
                        condition = condition && version <= versionInfo.MaxVersion;
                    }
                    else
                    {
                        condition = condition && version < versionInfo.MaxVersion;
                    }
                }

                return condition;
            };
        }

        /// <summary>
        /// Determines if the specified version is within the version spec
        /// </summary>
        public static bool Satisfies(this IVersionSpec versionSpec, SemanticVersion version)
        {
            // The range is unbounded so return true
            if (versionSpec == null)
            {
                return true;
            }
            return versionSpec.ToDelegate<SemanticVersion>(v => v)(version);
        }

        public static IEnumerable<string> GetComparableVersionStrings(this SemanticVersion version)
        {
            Version coreVersion = version.Version;
            string specialVersion = String.IsNullOrEmpty(version.SpecialVersion) ? String.Empty : "-" + version.SpecialVersion;

            var paths = new List<string>(3);

            paths.Add(String.Format(
                   CultureInfo.InvariantCulture,
                   "{0}.{1}.{2}.{3}{4}",
                   coreVersion.Major,
                   coreVersion.Minor,
                   coreVersion.Build,
                   coreVersion.Revision,
                   specialVersion));

            if (coreVersion.Revision == 0)
            {
                paths.Add(String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.{1}.{2}{3}",
                    coreVersion.Major,
                    coreVersion.Minor,
                    coreVersion.Build,
                    specialVersion));

                if (coreVersion.Build == 0)
                {
                    paths.Add(String.Format(
                        CultureInfo.InvariantCulture,
                        "{0}.{1}{2}",
                        coreVersion.Major,
                        coreVersion.Minor,
                        specialVersion));
                }
            }

            return paths;
        }
    }
}
