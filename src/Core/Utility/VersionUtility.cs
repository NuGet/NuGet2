using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using NuGet.Resources;

using CompatibilityMapping = System.Collections.Generic.Dictionary<string, string[]>;

namespace NuGet
{
    public static class VersionUtility
    {
        private const string NetFrameworkIdentifier = ".NETFramework";
        private const string WinRTFrameworkIdentifier = ".NETCore";
        private const string LessThanOrEqualTo = "\u2264";
        private const string GreaterThanOrEqualTo = "\u2265";

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Security", 
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification="The type FrameworkName is immutable.")]
        public static readonly FrameworkName UnsupportedFrameworkName = new FrameworkName("Unsupported", new Version());
        private static readonly Version _emptyVersion = new Version();

        private static readonly Dictionary<string, string> _knownIdentifiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "NET", NetFrameworkIdentifier },
            { ".NET", NetFrameworkIdentifier },
            { "NETFramework", NetFrameworkIdentifier },
            { ".NETFramework", NetFrameworkIdentifier },
            { "NETCore", WinRTFrameworkIdentifier},
            { ".NETCore", WinRTFrameworkIdentifier},
            { "WinRT", WinRTFrameworkIdentifier},
            { ".NETMicroFramework", ".NETMicroFramework" },
            { "netmf", ".NETMicroFramework" },
            { "SL", "Silverlight" },
            { "Silverlight", "Silverlight" }
        };

        private static readonly Dictionary<string, string> _knownProfiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "Client", "Client" },
            { "WP", "WindowsPhone" },
            { "WP71", "WindowsPhone71" },
            { "CF", "CompactFramework" },
            { "Full", String.Empty }
        };

        private static readonly Dictionary<string, string> _identifierToFrameworkFolder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { NetFrameworkIdentifier, "net" },
            { ".NETMicroFramework", "netmf" },
            { "Silverlight", "sl" }
        };

        private static readonly Dictionary<string, string> _identifierToProfileFolder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "WindowsPhone", "wp" },
            { "WindowsPhone71", "wp71" },
            { "CompactFramework", "cf" }
        };

        private static readonly Dictionary<string, CompatibilityMapping> _compatibiltyMapping = new Dictionary<string, CompatibilityMapping>(StringComparer.OrdinalIgnoreCase) {
            { 
                // Client profile is compatible with the full framework (empty string is full)
                NetFrameworkIdentifier, new CompatibilityMapping(StringComparer.OrdinalIgnoreCase) {
                    { "", new [] { "Client" } },
                    { "Client", new [] { "" } }
                }
            },
            {
                "Silverlight", new CompatibilityMapping(StringComparer.OrdinalIgnoreCase) {
                    { "WindowsPhone", new[] { "WindowsPhone71" } }
                }
            }
        };

        public static Version DefaultTargetFrameworkVersion
        {
            get
            {
                // We need to parse the version name out from the mscorlib's assembly name since
                // we can't call GetName() in medium trust
                return typeof(string).Assembly.GetNameSafe().Version;
            }
        }

        public static FrameworkName DefaultTargetFramework
        {
            get
            {
                return new FrameworkName(NetFrameworkIdentifier, DefaultTargetFrameworkVersion);
            }
        }

        /// <summary>
        /// This function tries to normalize a string that represents framework version names into
        /// something a framework name that the package manager understands.
        /// </summary>
        public static FrameworkName ParseFrameworkName(string frameworkName)
        {
            if (frameworkName == null)
            {
                throw new ArgumentNullException("frameworkName");
            }

            // {Identifier}{Version}-{Profile}

            // Split the framework name into 3 parts, identifier, version and profile.
            string identifierPart = null;
            string versionPart = null;

            string[] parts = frameworkName.Split('-');

            if (parts.Length > 2)
            {
                throw new ArgumentException(NuGetResources.InvalidFrameworkNameFormat, "frameworkName");
            }

            string frameworkNameAndVersion = parts.Length > 0 ? parts[0].Trim() : null;
            string profilePart = parts.Length > 1 ? parts[1].Trim() : null;

            if (String.IsNullOrEmpty(frameworkNameAndVersion))
            {
                throw new ArgumentException(NuGetResources.MissingFrameworkName, "frameworkName");
            }

            // If we find a version then we try to split the framework name into 2 parts
            var versionMatch = Regex.Match(frameworkNameAndVersion, @"\d+");

            if (versionMatch.Success)
            {
                identifierPart = frameworkNameAndVersion.Substring(0, versionMatch.Index).Trim();
                versionPart = frameworkNameAndVersion.Substring(versionMatch.Index).Trim();
            }
            else
            {
                // Otherwise we take the whole name as an identifier
                identifierPart = frameworkNameAndVersion.Trim();
            }

            if (!String.IsNullOrEmpty(identifierPart))
            {
                // Try to normalize the identifier to a known identifier
                if (!_knownIdentifiers.TryGetValue(identifierPart, out identifierPart))
                {
                    return UnsupportedFrameworkName;
                }
            }

            if (!String.IsNullOrEmpty(profilePart))
            {
                string knownProfile;
                if (_knownProfiles.TryGetValue(profilePart, out knownProfile))
                {
                    profilePart = knownProfile;
                }
            }

            Version version = null;
            // We support version formats that are integers (40 becomes 4.0)
            int versionNumber;
            if (Int32.TryParse(versionPart, out versionNumber))
            {
                // Remove the extra numbers
                if (versionPart.Length > 4)
                {
                    versionPart = versionPart.Substring(0, 4);
                }

                // Make sure it has at least 2 digits so it parses as a valid version
                versionPart = versionPart.PadRight(2, '0');
                versionPart = String.Join(".", versionPart.Select(ch => ch.ToString(CultureInfo.InvariantCulture)));
            }

            // If we can't parse the version then use the default
            if (!Version.TryParse(versionPart, out version))
            {
                // We failed to parse the version string once more. So we need to decide if this is unsupported or if we use the default version.
                // This framework is unsupported if:
                // 1. The identifier part of the framework name is null.
                // 2. The version part is not null.
                if (String.IsNullOrEmpty(identifierPart) || !String.IsNullOrEmpty(versionPart))
                {
                    return UnsupportedFrameworkName;
                }

                version = _emptyVersion;
            }

            if (String.IsNullOrEmpty(identifierPart))
            {
                identifierPart = NetFrameworkIdentifier;
            }

            return new FrameworkName(identifierPart, version, profilePart);
        }

        /// <summary>
        /// Trims trailing zeros in revision and build.
        /// </summary>
        public static Version TrimVersion(Version version)
        {
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            if (version.Build == 0 && version.Revision == 0)
            {
                version = new Version(version.Major, version.Minor);
            }
            else if (version.Revision == 0)
            {
                version = new Version(version.Major, version.Minor, version.Build);
            }

            return version;
        }

        /// <summary>
        /// The version string is either a simple version or an arithmetic range
        /// e.g.
        ///      1.0         --> 1.0 ≤ x
        ///      (,1.0]      --> x ≤ 1.0
        ///      (,1.0)      --> x &lt; 1.0
        ///      [1.0]       --> x == 1.0
        ///      (1.0,)      --> 1.0 &lt; x
        ///      (1.0, 2.0)   --> 1.0 &lt; x &lt; 2.0
        ///      [1.0, 2.0]   --> 1.0 ≤ x ≤ 2.0
        /// </summary>
        public static IVersionSpec ParseVersionSpec(string value)
        {
            IVersionSpec versionInfo;
            if (!TryParseVersionSpec(value, out versionInfo))
            {
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture,
                     NuGetResources.InvalidVersionString, value));
            }

            return versionInfo;
        }

        public static bool TryParseVersionSpec(string value, out IVersionSpec result)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var versionSpec = new VersionSpec();
            value = value.Trim();

            // First, try to parse it as a plain version string
            SemanticVersion version;
            if (SemanticVersion.TryParse(value, out version))
            {
                // A plain version is treated as an inclusive minimum range
                result = new VersionSpec
                {
                    MinVersion = version,
                    IsMinInclusive = true
                };

                return true;
            }

            // It's not a plain version, so it must be using the bracket arithmetic range syntax

            result = null;

            // Fail early if the string is too short to be valid
            if (value.Length < 3)
            {
                return false;
            }

            // The first character must be [ ot (
            switch (value.First())
            {
                case '[':
                    versionSpec.IsMinInclusive = true;
                    break;
                case '(':
                    versionSpec.IsMinInclusive = false;
                    break;
                default:
                    return false;
            }

            // The last character must be ] ot )
            switch (value.Last())
            {
                case ']':
                    versionSpec.IsMaxInclusive = true;
                    break;
                case ')':
                    versionSpec.IsMaxInclusive = false;
                    break;
                default:
                    return false;
            }

            // Get rid of the two brackets
            value = value.Substring(1, value.Length - 2);

            // Split by comma, and make sure we don't get more than two pieces
            string[] parts = value.Split(',');
            if (parts.Length > 2)
            {
                return false;
            }
            else if (parts.All(String.IsNullOrEmpty))
            {
                // If all parts are empty, then neither of upper or lower bounds were specified. Version spec is of the format (,]
                return false;
            }


            // If there is only one piece, we use it for both min and max
            string minVersionString = parts[0];
            string maxVersionString = (parts.Length == 2) ? parts[1] : parts[0];

            // Only parse the min version if it's non-empty
            if (!String.IsNullOrWhiteSpace(minVersionString))
            {
                if (!TryParseVersion(minVersionString, out version))
                {
                    return false;
                }
                versionSpec.MinVersion = version;
            }

            // Same deal for max
            if (!String.IsNullOrWhiteSpace(maxVersionString))
            {
                if (!TryParseVersion(maxVersionString, out version))
                {
                    return false;
                }
                versionSpec.MaxVersion = version;
            }

            // Successful parse!
            result = versionSpec;
            return true;
        }

        /// <summary>
        /// The safe range is defined as the highest build and revision for a given major and minor version
        /// </summary>
        public static IVersionSpec GetSafeRange(SemanticVersion version)
        {
            return new VersionSpec
            {
                IsMinInclusive = true,
                MinVersion = version,
                MaxVersion = new SemanticVersion(new Version(version.Version.Major, version.Version.Minor + 1))
            };
        }

        public static string PrettyPrint(IVersionSpec versionSpec)
        {
            if (versionSpec.MinVersion != null && versionSpec.IsMinInclusive && versionSpec.MaxVersion == null && !versionSpec.IsMaxInclusive)
            {
                return String.Format(CultureInfo.InvariantCulture, "({0} {1})", GreaterThanOrEqualTo, versionSpec.MinVersion);
            }

            if (versionSpec.MinVersion != null && versionSpec.MaxVersion != null && versionSpec.MinVersion == versionSpec.MaxVersion && versionSpec.IsMinInclusive && versionSpec.IsMaxInclusive)
            {
                return String.Format(CultureInfo.InvariantCulture, "(= {0})", versionSpec.MinVersion);
            }

            var versionBuilder = new StringBuilder();
            if (versionSpec.MinVersion != null)
            {
                if (versionSpec.IsMinInclusive)
                {
                    versionBuilder.AppendFormat(CultureInfo.InvariantCulture, "({0} ", GreaterThanOrEqualTo);
                }
                else
                {
                    versionBuilder.Append("(> ");
                }
                versionBuilder.Append(versionSpec.MinVersion);
            }

            if (versionSpec.MaxVersion != null)
            {
                if (versionBuilder.Length == 0)
                {
                    versionBuilder.Append("(");
                }
                else
                {
                    versionBuilder.Append(" && ");
                }

                if (versionSpec.IsMaxInclusive)
                {
                    versionBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0} ", LessThanOrEqualTo);
                }
                else
                {
                    versionBuilder.Append("< ");
                }
                versionBuilder.Append(versionSpec.MaxVersion);
            }

            if (versionBuilder.Length > 0)
            {
                versionBuilder.Append(")");
            }

            return versionBuilder.ToString();
        }

        public static string GetFrameworkString(FrameworkName frameworkName)
        {
            string name = frameworkName.Identifier + frameworkName.Version;
            if (String.IsNullOrEmpty(frameworkName.Profile))
            {
                return name;
            }
            return name + "-" + frameworkName.Profile;
        }

        public static string GetShortFrameworkName(FrameworkName frameworkName)
        {
            string name;
            if (!_identifierToFrameworkFolder.TryGetValue(frameworkName.Identifier, out name))
            {
                name = frameworkName.Identifier;
            }

            // Remove the . from versions
            name += frameworkName.Version.ToString().Replace(".", String.Empty);

            if (String.IsNullOrEmpty(frameworkName.Profile))
            {
                return name;
            }

            string profile;
            if (!_identifierToProfileFolder.TryGetValue(frameworkName.Profile, out profile))
            {
                profile = frameworkName.Profile;
            }

            return name + "-" + profile;
        }

        internal static FrameworkName ParseFrameworkFolderName(string path)
        {
            // The path for a reference might look like this for assembly foo.dll:            
            // foo.dll
            // sub\foo.dll
            // {FrameworkName}{Version}\foo.dll
            // {FrameworkName}{Version}\sub1\foo.dll
            // {FrameworkName}{Version}\sub1\sub2\foo.dll

            // Get the target framework string if specified
            string targetFrameworkString = Path.GetDirectoryName(path).Split(Path.DirectorySeparatorChar).FirstOrDefault();

            if (!String.IsNullOrEmpty(targetFrameworkString))
            {
                return VersionUtility.ParseFrameworkName(targetFrameworkString);
            }

            return null;
        }

        public static bool TryGetCompatibleItems<T>(FrameworkName projectFramework, IEnumerable<T> items, out IEnumerable<T> compatibleItems) where T : IFrameworkTargetable
        {
            if (!items.Any())
            {
                compatibleItems = Enumerable.Empty<T>();
                return true;
            }

            // Default framework for assembly references with an unspecified framework name
            // always match the project framework's identifier by is the lowest possible version
            var defaultFramework = new FrameworkName(projectFramework.Identifier, new Version(), projectFramework.Profile);

            // Turn something that looks like this:
            // item -> [Framework1, Framework2, Framework3] into
            // [{item, Framework1}, {item, Framework2}, {item, Framework3}]
            var normalizedItems = from item in items
                                  let frameworks = item.SupportedFrameworks.Any() ? item.SupportedFrameworks : new FrameworkName[] { null }
                                  from framework in frameworks
                                  select new
                                  {
                                      Item = item,
                                      TargetFramework = framework
                                  };

            // Group references by target framework (if there is no target framework we assume it is the default)
            var frameworkGroups = normalizedItems.GroupBy(g => g.TargetFramework ?? defaultFramework, g => g.Item);

            // Try to find the best match
            compatibleItems = (from g in frameworkGroups
                               where IsCompatible(projectFramework, g.Key)
                               orderby GetProfileCompatibility(projectFramework, g.Key) descending,
                                       g.Key.Version descending
                               select g).FirstOrDefault();

            return compatibleItems != null && compatibleItems.Any();
        }

        internal static Version NormalizeVersion(Version verison)
        {
            return new Version(verison.Major,
                               verison.Minor,
                               Math.Max(verison.Build, 0),
                               Math.Max(verison.Revision, 0));
        }

        /// <summary>
        /// Returns all possible versions for a version. i.e. 1.0 would return 1.0, 1.0.0, 1.0.0.0
        /// </summary>
        internal static IEnumerable<SemanticVersion> GetPossibleVersions(SemanticVersion semVer)
        {
            // Trim the version so things like 1.0.0.0 end up being 1.0
            Version version = TrimVersion(semVer.Version);

            yield return semVer;
            if (version.Build == -1 && version.Revision == -1)
            {
                yield return new SemanticVersion(new Version(version.Major, version.Minor, 0), semVer.SpecialVersion);
                yield return new SemanticVersion(new Version(version.Major, version.Minor, 0, 0), semVer.SpecialVersion);
            }
            else if (version.Revision == -1)
            {
                yield return new SemanticVersion(new Version(version.Major, version.Minor, version.Build, 0), semVer.SpecialVersion);
            }
        }

        public static bool IsCompatible(FrameworkName frameworkName, IEnumerable<FrameworkName> supportedFrameworks)
        {
            if (supportedFrameworks.Any())
            {
                return supportedFrameworks.Any(supportedFramework => IsCompatible(frameworkName, supportedFramework));
            }

            // No supported frameworks means that everything is supported.
            return true;
        }

        internal static bool IsCompatible(FrameworkName frameworkName, FrameworkName targetFrameworkName)
        {
            if (!frameworkName.Identifier.Equals(targetFrameworkName.Identifier, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (NormalizeVersion(frameworkName.Version) <
                NormalizeVersion(targetFrameworkName.Version))
            {
                return false;
            }

            // If the profile names are equal then they're compatible
            if (String.Equals(frameworkName.Profile, targetFrameworkName.Profile, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Get the compatibility mapping for this framework identifier
            CompatibilityMapping mapping;
            if (_compatibiltyMapping.TryGetValue(frameworkName.Identifier, out mapping))
            {
                // Get all compatible profiles for the target profile
                string[] compatibleProfiles;
                if (mapping.TryGetValue(targetFrameworkName.Profile, out compatibleProfiles))
                {
                    // See if this profile is in the list of compatible profiles
                    return compatibleProfiles.Contains(frameworkName.Profile, StringComparer.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        /// <summary>
        /// Given 2 framework names, this method returns a number which determines how compatible
        /// the names are. The higher the number the more compatible the frameworks are.
        /// </summary>
        private static int GetProfileCompatibility(FrameworkName frameworkName, FrameworkName targetFrameworkName)
        {
            int compatibility = 0;

            if (NormalizeVersion(frameworkName.Version) == NormalizeVersion(targetFrameworkName.Version))
            {
                compatibility++;
            }

            // Things with matching profiles are more compatible than things without.
            // This means that if we have net40 and net40-client assemblies and the target framework is
            // net40, both sets of assemblies are compatible but we prefer net40 since it matches
            // the profile exactly.
            if (targetFrameworkName.Profile.Equals(frameworkName.Profile, StringComparison.OrdinalIgnoreCase))
            {
                compatibility++;
            }

            return compatibility;
        }

        private static bool TryParseVersion(string versionString, out SemanticVersion version)
        {
            version = null;
            if (!SemanticVersion.TryParse(versionString, out version))
            {
                // Support integer version numbers (i.e. 1 -> 1.0)
                int versionNumber;
                if (Int32.TryParse(versionString, out versionNumber) && versionNumber > 0)
                {
                    version = new SemanticVersion(new Version(versionNumber, 0));
                }
            }
            return version != null;
        }
    }
}