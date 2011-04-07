using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using NuGet.Resources;

namespace NuGet {
    public static class VersionUtility {
        private const string NetFrameworkIdentifier = ".NETFramework";
        private static readonly FrameworkName UnsupportedFrameworkName = new FrameworkName("Unsupported", new Version());

        private static readonly Dictionary<string, string> _knownIdentifiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "NET", NetFrameworkIdentifier },
            { ".NET", NetFrameworkIdentifier },
            { "NETFramework", NetFrameworkIdentifier },
            { ".NETFramework", NetFrameworkIdentifier },
            { ".NETMicroFramework", ".NETMicroFramework" },
            { "netmf", ".NETMicroFramework" },
            { "SL", "Silverlight" },
            { "Silverlight", "Silverlight" }
        };

        private static readonly Dictionary<string, string> _knownProfiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "Client", "Client" },
            { "WP", "WindowsPhone" },
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
            { "CompactFramework", "cf" }
        };

        public static Version DefaultTargetFrameworkVersion {
            get {
                // We need to parse the version name out from the mscorlib's assembly name since
                // we can't call GetName() in medium trust
                return typeof(string).Assembly.GetNameSafe().Version;
            }
        }

        public static FrameworkName DefaultTargetFramework {
            get {
                return new FrameworkName(NetFrameworkIdentifier, DefaultTargetFrameworkVersion);
            }
        }

        public static Version ParseOptionalVersion(string version) {
            Version versionValue;
            if (!String.IsNullOrEmpty(version) && Version.TryParse(version, out versionValue)) {
                return versionValue;
            }
            return null;
        }

        /// <summary>
        /// This function tries to normalize a string that represents framework version names into
        /// something a framework name that the package manager understands.
        /// </summary>
        public static FrameworkName ParseFrameworkName(string frameworkName) {
            if (frameworkName == null) {
                throw new ArgumentNullException("frameworkName");
            }

            // {Identifier}{Version}-{Profile}

            // Split the framework name into 3 parts, identifier, version and profile.
            string identifierPart = null;
            string versionPart = null;

            string[] parts = frameworkName.Split('-');

            if (parts.Length > 2) {
                throw new ArgumentException(NuGetResources.InvalidFrameworkNameFormat, "frameworkName");
            }

            string frameworkNameAndVersion = parts.Length > 0 ? parts[0].Trim() : null;
            string profilePart = parts.Length > 1 ? parts[1].Trim() : null;

            if (String.IsNullOrEmpty(frameworkNameAndVersion)) {
                throw new ArgumentException(NuGetResources.MissingFrameworkName, "frameworkName");
            }

            // If we find a version then we try to split the framework name into 2 parts
            var match = Regex.Match(frameworkNameAndVersion, @"\d+");

            if (match.Success) {
                identifierPart = frameworkNameAndVersion.Substring(0, match.Index).Trim();
                versionPart = frameworkNameAndVersion.Substring(match.Index).Trim();
            }
            else {
                // Otherwise we take the whole name as an identifier
                identifierPart = frameworkNameAndVersion.Trim();
            }

            if (!String.IsNullOrEmpty(identifierPart)) {
                // Try to nomalize the identifier to a known identifier
                if (!_knownIdentifiers.TryGetValue(identifierPart, out identifierPart)) {
                    return UnsupportedFrameworkName;
                }
            }

            if (!String.IsNullOrEmpty(profilePart)) {
                string knownProfile;
                if (_knownProfiles.TryGetValue(profilePart, out knownProfile)) {
                    profilePart = knownProfile;
                }
            }

            Version version = null;
            // We support version formats that are integers (40 becomes 4.0)
            int versionNumber;
            if (Int32.TryParse(versionPart, out versionNumber)) {
                // Remove the extra numbers
                if (versionPart.Length > 4) {
                    versionPart = versionPart.Substring(0, 4);
                }

                // Make sure it has at least 2 digits so it parses as a valid version
                versionPart = versionPart.PadRight(2, '0');
                versionPart = String.Join(".", versionPart.Select(ch => ch.ToString()));
            }

            // If we can't parse the version then use the default
            if (!Version.TryParse(versionPart, out version)) {
                if (String.IsNullOrEmpty(identifierPart)) {
                    return UnsupportedFrameworkName;
                }

                version = DefaultTargetFrameworkVersion;
            }

            if (String.IsNullOrEmpty(identifierPart)) {
                identifierPart = NetFrameworkIdentifier;
            }

            return new FrameworkName(identifierPart, version, profilePart);
        }

        /// <summary>
        /// Trims trailing zeros in revision and build.
        /// </summary>
        public static Version TrimVersion(Version version) {
            if (version == null) {
                throw new ArgumentNullException("version");
            }

            if (version.Build == 0 && version.Revision == 0) {
                version = new Version(version.Major, version.Minor);
            }
            else if (version.Revision == 0) {
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
        public static IVersionSpec ParseVersionSpec(string value) {
            IVersionSpec versionInfo;
            if (!TryParseVersionSpec(value, out versionInfo)) {
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture,
                     NuGetResources.InvalidVersionString, value));
            }

            return versionInfo;
        }

        public static bool TryParseVersionSpec(string value, out IVersionSpec result) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            var versionSpec = new VersionSpec();
            value = value.Trim();

            // First, try to parse it as a plain version string
            Version version;
            if (Version.TryParse(value, out version)) {
                // A plain version is treated as an inclusive minimum range
                result = new VersionSpec {
                    MinVersion = version,
                    IsMinInclusive = true
                };

                return true;
            }

            // It's not a plain version, so it must be using the bracket arithmetic range syntax

            result = null;

            // Fail early if the string is too short to be valid
            if (value.Length < 3) {
                return false;
            }

            // The first character must be [ ot (
            switch (value.First()) {
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
            switch (value.Last()) {
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
            if (parts.Length > 2) {
                return false;
            }

            // If there is only one piece, we use it for both min and max
            string minVersionString = parts[0];
            string maxVersionString = (parts.Length == 2) ? parts[1] : parts[0];

            // Only parse the min version if it's non-empty
            if (!String.IsNullOrWhiteSpace(minVersionString)) {
                if (!Version.TryParse(minVersionString, out version)) {
                    return false;
                }
                versionSpec.MinVersion = version;
            }

            // Same deal for max
            if (!String.IsNullOrWhiteSpace(maxVersionString)) {
                if (!Version.TryParse(maxVersionString, out version)) {
                    return false;
                }
                versionSpec.MaxVersion = version;
            }

            // Successful parse!
            result = versionSpec;
            return true;
        }

        public static string GetFrameworkString(FrameworkName frameworkName) {
            string name = frameworkName.Identifier + frameworkName.Version;
            if (String.IsNullOrEmpty(frameworkName.Profile)) {
                return name;
            }
            return name + "-" + frameworkName.Profile;
        }

        public static string GetFrameworkFolder(FrameworkName frameworkName) {
            string name;
            if (!_identifierToFrameworkFolder.TryGetValue(frameworkName.Identifier, out name)) {
                name = frameworkName.Identifier;
            }

            // Remove the . from versions
            name += frameworkName.Version.ToString().Replace(".", String.Empty);

            if (String.IsNullOrEmpty(frameworkName.Profile)) {
                return name;
            }

            string profile;
            if (!_identifierToProfileFolder.TryGetValue(frameworkName.Profile, out profile)) {
                profile = frameworkName.Profile;
            }

            return name + "-" + profile;
        }

        internal static FrameworkName ParseFrameworkFolderName(string path) {
            // The path for a reference might look like this for assembly foo.dll:            
            // foo.dll
            // sub\foo.dll
            // {FrameworkName}{Version}\foo.dll
            // {FrameworkName}{Version}\sub1\foo.dll
            // {FrameworkName}{Version}\sub1\sub2\foo.dll

            // Get the target framework string if specified
            string targetFrameworkString = Path.GetDirectoryName(path).Split(Path.DirectorySeparatorChar).FirstOrDefault();

            if (!String.IsNullOrEmpty(targetFrameworkString)) {
                return VersionUtility.ParseFrameworkName(targetFrameworkString);
            }

            return null;
        }

        public static bool TryGetCompatibleItems<T>(FrameworkName projectFramework, IEnumerable<T> items, out IEnumerable<T> compatibleItems) where T : IFrameworkTargetable {
            if (!items.Any()) {
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
                                  select new {
                                      Item = item,
                                      TargetFramework = framework
                                  };

            // Group references by target framework (if there is no target framework we assume it is the default)
            var frameworkGroups = normalizedItems.GroupBy(g => g.TargetFramework ?? defaultFramework, g => g.Item);

            // Try to find the best match
            compatibleItems = (from g in frameworkGroups
                               where IsCompatible(g.Key, projectFramework)
                               orderby GetProfileCompatibility(g.Key, projectFramework) descending,
                                       g.Key.Version descending
                               select g).FirstOrDefault();

            return compatibleItems != null && compatibleItems.Any();
        }

        private static bool IsCompatible(FrameworkName frameworkName, FrameworkName targetFrameworkName) {
            if (!frameworkName.Identifier.Equals(targetFrameworkName.Identifier, StringComparison.OrdinalIgnoreCase)) {
                return false;
            }

            if (frameworkName.Version > targetFrameworkName.Version) {
                return false;
            }

            // If there is no target framework then do nothing
            if (String.IsNullOrEmpty(targetFrameworkName.Profile)) {
                return true;
            }

            string targetProfile = frameworkName.Profile;

            if (String.IsNullOrEmpty(targetProfile)) {
                // We consider net40 to mean net40-full which is a superset of any specific profile.
                // This means that a dll that is net40 will work for a project targeting net40-client.
                targetProfile = targetFrameworkName.Profile;
            }

            return targetFrameworkName.Profile.Equals(targetProfile, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Given 2 framework names, this method returns a number which determines how compatible
        /// the names are. The higher the number the more compatible the frameworks are.
        /// </summary>
        private static int GetProfileCompatibility(FrameworkName frameworkName, FrameworkName targetFrameworkName) {
            // Things with matching profiles are more compatible than things without.
            // This means that if we have net40 and net40-client assemblies and the target framework is
            // net40, both sets of assemblies are compatible but we prefer net40 since it matches
            // the profile exactly.
            if (targetFrameworkName.Profile.Equals(frameworkName.Profile, StringComparison.OrdinalIgnoreCase)) {
                return 1;
            }

            return 0;
        }
    }
}
