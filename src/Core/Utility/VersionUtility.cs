using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using NuGet.Frameworks;
using NuGet.Resources;

namespace NuGet
{
    public static class VersionUtility
    {
        private const string NetFrameworkIdentifier = ".NETFramework";
        private const string NetCoreAppFrameworkShortName = "netcoreapp";
        private const string NetCoreAppFrameworkIdentifier = ".NETCoreApp";
        private const string NetCoreFrameworkIdentifier = ".NETCore";
        private const string PortableFrameworkIdentifier = ".NETPortable";
        private const string NetPlatformFrameworkIdentifier = ".NETPlatform";
        private const string NetPlatformFrameworkShortName = "dotnet";
        private const string NetStandardFrameworkShortName = "netstandard";
        private const string NetStandardFrameworkIdentifier = ".NETStandard";
        private const string NetStandardAppFrameworkShortName = "netstandardapp";
        private const string NetStandardAppFrameworkIdentifier = ".NETStandardApp";
        private const string AspNetFrameworkIdentifier = "ASP.Net";
        private const string AspNetCoreFrameworkIdentifier = "ASP.NetCore";
        private const string DnxFrameworkIdentifier = "DNX";
        private const string DnxFrameworkShortName = "dnx";
        private const string DnxCoreFrameworkIdentifier = "DNXCore";
        private const string DnxCoreFrameworkShortName = "dnxcore";
        private const string UAPFrameworkIdentifier = "UAP";
        private const string UAPFrameworkShortName = "uap";
        private const string LessThanOrEqualTo = "\u2264";
        private const string GreaterThanOrEqualTo = "\u2265";

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "The type FrameworkName is immutable.")]
        public static readonly FrameworkName EmptyFramework = new FrameworkName("NoFramework", new Version());

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "The type FrameworkName is immutable.")]
        public static readonly FrameworkName NativeProjectFramework = new FrameworkName("Native", new Version());

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "The type FrameworkName is immutable.")]
        public static readonly FrameworkName UnsupportedFrameworkName = new FrameworkName("Unsupported", new Version());
        private static readonly Version _emptyVersion = new Version();

        private static readonly Dictionary<string, string> _knownIdentifiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            // FYI, the keys are CASE-INSENSITIVE

            // .NET Desktop
            { "NET", NetFrameworkIdentifier },
            { ".NET", NetFrameworkIdentifier },
            { "NETFramework", NetFrameworkIdentifier },
            { ".NETFramework", NetFrameworkIdentifier },

            // .NET Core
            { "NETCore", NetCoreFrameworkIdentifier},
            { ".NETCore", NetCoreFrameworkIdentifier},
            { "WinRT", NetCoreFrameworkIdentifier},     // 'WinRT' is now deprecated. Use 'Windows' or 'win' instead.

            // .NET Micro Framework
            { ".NETMicroFramework", ".NETMicroFramework" },
            { "netmf", ".NETMicroFramework" },

            // Silverlight
            { "SL", "Silverlight" },
            { "Silverlight", "Silverlight" },

            // Portable Class Libraries
            { ".NETPortable", PortableFrameworkIdentifier },
            { "NETPortable", PortableFrameworkIdentifier },
            { "portable", PortableFrameworkIdentifier },

            // Windows Phone
            { "wp", "WindowsPhone" },
            { "WindowsPhone", "WindowsPhone" },
            { "WindowsPhoneApp", "WindowsPhoneApp"},
            { "wpa", "WindowsPhoneApp"},

            // Windows
            { "Windows", "Windows" },
            { "win", "Windows" },

            // ASP.Net (TODO: Remove these eventually)
            { "aspnet", AspNetFrameworkIdentifier },
            { "aspnetcore", AspNetCoreFrameworkIdentifier },
            { "asp.net", AspNetFrameworkIdentifier },
            { "asp.netcore", AspNetCoreFrameworkIdentifier },

            // DNX
            { DnxFrameworkShortName, DnxFrameworkIdentifier },
            { DnxCoreFrameworkShortName, DnxCoreFrameworkIdentifier },

            // Dotnet
            { NetPlatformFrameworkShortName, NetPlatformFrameworkIdentifier },
            { NetPlatformFrameworkIdentifier, NetPlatformFrameworkIdentifier },

            // Netstandard
            { NetStandardFrameworkShortName, NetStandardFrameworkIdentifier },
            { NetStandardFrameworkIdentifier, NetStandardFrameworkIdentifier },

            // Netstandardapp
            { NetStandardAppFrameworkShortName, NetStandardAppFrameworkIdentifier },
            { NetStandardAppFrameworkIdentifier, NetStandardAppFrameworkIdentifier },

            // Netcoreapp
            { NetCoreAppFrameworkShortName, NetCoreAppFrameworkIdentifier },
            { NetCoreAppFrameworkIdentifier, NetCoreAppFrameworkIdentifier },

            // UAP
            { UAPFrameworkShortName, UAPFrameworkIdentifier },

            // Native
            { "native", "native"},

            // Mono/Xamarin
            { "MonoAndroid", "MonoAndroid" },
            { "MonoTouch", "MonoTouch" },
            { "MonoMac", "MonoMac" },
            { "Xamarin.iOS", "Xamarin.iOS" },
            { "XamariniOS", "Xamarin.iOS" },
            { "Xamarin.Mac", "Xamarin.Mac" },
            { "XamarinMac", "Xamarin.Mac" },
            { "Xamarin.PlayStationThree", "Xamarin.PlayStation3" },
            { "XamarinPlayStationThree", "Xamarin.PlayStation3" },
            { "XamarinPSThree", "Xamarin.PlayStation3" },
            { "Xamarin.PlayStationFour", "Xamarin.PlayStation4" },
            { "XamarinPlayStationFour", "Xamarin.PlayStation4" },
            { "XamarinPSFour", "Xamarin.PlayStation4" },
            { "Xamarin.PlayStationVita", "Xamarin.PlayStationVita" },
            { "XamarinPlayStationVita", "Xamarin.PlayStationVita" },
            { "XamarinPSVita", "Xamarin.PlayStationVita" },
            { "Xamarin.TVOS", "Xamarin.TVOS" },
            { "XamarinTVOS", "Xamarin.TVOS" },
            { "Xamarin.WatchOS", "Xamarin.WatchOS" },
            { "XamarinWatchOS", "Xamarin.WatchOS" },
            { "Xamarin.XboxThreeSixty", "Xamarin.Xbox360" },
            { "XamarinXboxThreeSixty", "Xamarin.Xbox360" },
            { "Xamarin.XboxOne", "Xamarin.XboxOne" },
            { "XamarinXboxOne", "Xamarin.XboxOne" }
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
            { DnxFrameworkIdentifier, DnxFrameworkShortName },
            { DnxCoreFrameworkIdentifier, DnxCoreFrameworkShortName },
            { NetPlatformFrameworkIdentifier, NetPlatformFrameworkShortName },
            { NetStandardFrameworkIdentifier, NetStandardFrameworkShortName },
            { NetStandardAppFrameworkIdentifier, NetStandardAppFrameworkShortName },
            { NetCoreAppFrameworkIdentifier, NetCoreAppFrameworkShortName },
            { AspNetFrameworkIdentifier, "aspnet" },
            { AspNetCoreFrameworkIdentifier, "aspnetcore" },
            { "Silverlight", "sl" },
            { ".NETCore45", "win"},
            { ".NETCore451", "win81"},
            { "Windows", "win"},
            { ".NETPortable", "portable" },
            { "WindowsPhone", "wp"},
            { "WindowsPhoneApp", "wpa"},
            { "Xamarin.iOS", "xamarinios" },
            { "Xamarin.Mac", "xamarinmac" },
            { "Xamarin.PlayStation3", "xamarinpsthree" },
            { "Xamarin.PlayStation4", "xamarinpsfour" },
            { "Xamarin.PlayStationVita", "xamarinpsvita" },
            { "Xamarin.TVOS", "xamarintvos" },
            { "Xamarin.WatchOS", "xamarinwatchos" },
            { "Xamarin.Xbox360", "xamarinxboxthreesixty" },
            { "Xamarin.XboxOne", "xamarinxboxone" },
        };

        private static readonly Dictionary<string, string> _identifierToProfileFolder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "WindowsPhone", "wp" },
            { "WindowsPhone71", "wp71" },
            { "CompactFramework", "cf" }
        };
        
        // These aliases allow us to accept 'wp', 'wp70', 'wp71', 'windows', 'windows8' as valid target farmework folders.
        private static readonly Dictionary<FrameworkName, FrameworkName> _frameworkNameAlias = new Dictionary<FrameworkName, FrameworkName>(FrameworkNameEqualityComparer.Default)
        {
            { new FrameworkName("WindowsPhone, Version=v0.0"), new FrameworkName("Silverlight, Version=v3.0, Profile=WindowsPhone") },
            { new FrameworkName("WindowsPhone, Version=v7.0"), new FrameworkName("Silverlight, Version=v3.0, Profile=WindowsPhone") },
            { new FrameworkName("WindowsPhone, Version=v7.1"), new FrameworkName("Silverlight, Version=v4.0, Profile=WindowsPhone71") },
            { new FrameworkName("WindowsPhone, Version=v8.0"), new FrameworkName("Silverlight, Version=v8.0, Profile=WindowsPhone") },
            { new FrameworkName("WindowsPhone, Version=v8.1"), new FrameworkName("Silverlight, Version=v8.1, Profile=WindowsPhone") },

            { new FrameworkName("Windows, Version=v0.0"), new FrameworkName(".NETCore, Version=v4.5") },
            { new FrameworkName("Windows, Version=v8.0"), new FrameworkName(".NETCore, Version=v4.5") },
            { new FrameworkName("Windows, Version=v8.1"), new FrameworkName(".NETCore, Version=v4.5.1") }
        };

        public static Version DefaultTargetFrameworkVersion
        {
            get
            {
                // We need to parse the version name out from the mscorlib's assembly name since
                // we can't call GetName() in medium trust
                return typeof(string).Assembly.GetName().Version;
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
                versionPart = String.Join(".", versionPart.ToCharArray());
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

                // Use 5.0 instead of 0.0 as the default for NetPlatform
                if (identifierPart.Equals(NetPlatformFrameworkIdentifier))
                {
                    version = new Version(5, 0);
                }
                else if (identifierPart.Equals(NetCoreAppFrameworkIdentifier))
                {
                    version = new Version(1, 0);
                }
                else
                {
                    version = _emptyVersion;
                }
            }

            if (String.IsNullOrEmpty(identifierPart))
            {
                identifierPart = NetFrameworkIdentifier;
            }

            // if this is a .NET Portable framework name, validate the profile part to ensure it is valid
            if (identifierPart.Equals(PortableFrameworkIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                ValidatePortableFrameworkProfilePart(profilePart);
            }

            return new FrameworkName(identifierPart, version, profilePart);
        }

        internal static void ValidatePortableFrameworkProfilePart(string profilePart)
        {
            if (String.IsNullOrEmpty(profilePart))
            {
                throw new ArgumentException(NuGetResources.PortableFrameworkProfileEmpty, "profilePart");
            }

            if (profilePart.Contains('-'))
            {
                throw new ArgumentException(NuGetResources.PortableFrameworkProfileHasDash, "profilePart");
            }

            if (profilePart.Contains(' '))
            {
                throw new ArgumentException(NuGetResources.PortableFrameworkProfileHasSpace, "profilePart");
            }

            string[] parts = profilePart.Split('+');
            if (parts.Any(p => String.IsNullOrEmpty(p)))
            {
                throw new ArgumentException(NuGetResources.PortableFrameworkProfileComponentIsEmpty, "profilePart");
            }

            // Prevent portable framework inside a portable framework - Inception
            if (parts.Any(p => p.StartsWith("portable", StringComparison.OrdinalIgnoreCase)) ||
                parts.Any(p => p.StartsWith("NETPortable", StringComparison.OrdinalIgnoreCase)) ||
                parts.Any(p => p.StartsWith(".NETPortable", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(NuGetResources.PortableFrameworkProfileComponentIsPortable, "profilePart");
            }
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
            return GetShortFrameworkName(NetPortableProfileTable.Instance, frameworkName);
        }

        internal static string GetShortFrameworkName(NetPortableProfileTable table, FrameworkName frameworkName)
        {
            if (frameworkName == null)
            {
                throw new ArgumentNullException("frameworkName");
            }

            // Do a reverse lookup in _frameworkNameAlias. This is so that we can produce the more user-friendly
            // "windowsphone" string, rather than "sl3-wp". The latter one is also prohibited in portable framework's profile string.
            foreach (KeyValuePair<FrameworkName, FrameworkName> pair in _frameworkNameAlias)
            {
                // use our custom equality comparer because we want to perform case-insensitive comparison
                if (FrameworkNameEqualityComparer.Default.Equals(pair.Value, frameworkName))
                {
                    frameworkName = pair.Key;
                    break;
                }
            }

            if (frameworkName.Version.Major == 5
                && frameworkName.Version.Minor == 0
                && frameworkName.Identifier.Equals(NetPlatformFrameworkIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                // Normalize version 5.0 to 0.0 for display purposes for dotnet
                frameworkName = new FrameworkName(frameworkName.Identifier, _emptyVersion, frameworkName.Profile);
            }

            string name;
            if (!_identifierToFrameworkFolder.TryGetValue(frameworkName.Identifier, out name))
            {
                name = frameworkName.Identifier;
            }

            // for Portable framework name, the short name has the form "portable-sl4+wp7+net45"
            string profile;
            if (name.Equals("portable", StringComparison.OrdinalIgnoreCase))
            {
                var portableProfile = NetPortableProfile.Parse(
                    table,
                    frameworkName.Profile);

                if (portableProfile != null)
                {
                    profile = portableProfile.CustomProfileString;
                }
                else
                {
                    profile = frameworkName.Profile;
                }
            }
            else
            {
                // only show version part if it's > 0.0.0.0
                if (frameworkName.Version > new Version())
                {
                    // Remove the . from versions
                    if (RequiresDecimalVersioning(frameworkName.Version))
                    {
                        // This version has digits over 10 and must be expressed using decimals
                        name += GetDecimalVersionString(frameworkName.Version);
                    }
                    else
                    {
                        if (frameworkName.Identifier.Equals(NetStandardAppFrameworkIdentifier, StringComparison.OrdinalIgnoreCase)
                            || frameworkName.Identifier.Equals(NetStandardFrameworkIdentifier, StringComparison.OrdinalIgnoreCase)
                            || frameworkName.Identifier.Equals(NetPlatformFrameworkIdentifier, StringComparison.OrdinalIgnoreCase)
                            || frameworkName.Identifier.Equals(NetCoreAppFrameworkIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            // do not remove the . from versions for dotnet/netstandard(app)/netcoreapp frameworks
                            name += frameworkName.Version.ToString();
                        }
                        else
                        {
                            // remove the . from versions
                            name += frameworkName.Version.ToString().Replace(".", string.Empty);
                        }
                    }
                }

                if (String.IsNullOrEmpty(frameworkName.Profile))
                {
                    return name;
                }

                if (!_identifierToProfileFolder.TryGetValue(frameworkName.Profile, out profile))
                {
                    profile = frameworkName.Profile;
                }
            }

            return name + "-" + profile;
        }

        private static bool RequiresDecimalVersioning(Version version)
        {
            return version != null
                && (version.Major > 9
                   || version.Minor > 9
                   || version.Build > 9
                   || version.Revision > 9);
        }

        private static string GetDecimalVersionString(Version version)
        {
            StringBuilder sb = new StringBuilder();

            if (version != null)
            {
                Stack<int> versionParts = new Stack<int>();

                versionParts.Push(version.Major > 0 ? version.Major : 0);
                versionParts.Push(version.Minor > 0 ? version.Minor : 0);
                versionParts.Push(version.Build > 0 ? version.Build : 0);
                versionParts.Push(version.Revision > 0 ? version.Revision : 0);

                // if any parts of the version are over 9 we need to use decimals
                bool useDecimals = RequiresDecimalVersioning(version);

                // remove all trailing zeros
                while (versionParts.Count > 0 && versionParts.Peek() <= 0)
                {
                    versionParts.Pop();
                }

                // write the version string out backwards
                while (versionParts.Count > 0)
                {
                    // avoid adding a decimal if this is the first digit, but if we are down
                    // to only 2 numbers left we have to add a decimal otherwise 10.0 becomes 1.0
                    // during the parse
                    if (useDecimals)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Insert(0, ".");
                        }
                        else if (versionParts.Count == 1)
                        {
                            sb.Append(".0");
                        }
                    }

                    sb.Insert(0, versionParts.Pop());
                }
            }

            return sb.ToString();
        }

        public static string GetTargetFrameworkLogString(FrameworkName targetFramework)
        {
            return (targetFramework == null || targetFramework == VersionUtility.EmptyFramework) ? NuGetResources.Debug_TargetFrameworkInfo_NotFrameworkSpecific : String.Empty;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public static FrameworkName ParseFrameworkNameFromFilePath(string filePath, out string effectivePath)
        {
            var knownFolders = new string[]
            {
                Constants.ContentDirectory,
                Constants.LibDirectory,
                Constants.ToolsDirectory,
                Constants.BuildDirectory
            };

            for (int i = 0; i < knownFolders.Length; i++)
            {
                string folderPrefix = knownFolders[i] + System.IO.Path.DirectorySeparatorChar;
                if (filePath.Length > folderPrefix.Length &&
                    filePath.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string frameworkPart = filePath.Substring(folderPrefix.Length);

                    try
                    {
                        return VersionUtility.ParseFrameworkFolderName(
                            frameworkPart,
                            strictParsing: knownFolders[i] == Constants.LibDirectory,
                            effectivePath: out effectivePath);
                    }
                    catch (ArgumentException)
                    {
                        // if the parsing fails, we treat it as if this file
                        // doesn't have target framework.
                        effectivePath = frameworkPart;
                        return null;
                    }
                }

            }

            effectivePath = filePath;
            return null;
        }

        public static FrameworkName ParseFrameworkFolderName(string path)
        {
            string effectivePath;
            return ParseFrameworkFolderName(path, strictParsing: true, effectivePath: out effectivePath);
        }

        /// <summary>
        /// Parses the specified string into FrameworkName object.
        /// </summary>
        /// <param name="path">The string to be parse.</param>
        /// <param name="strictParsing">if set to <c>true</c>, parse the first folder of path even if it is unrecognized framework.</param>
        /// <param name="effectivePath">returns the path after the parsed target framework</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        public static FrameworkName ParseFrameworkFolderName(string path, bool strictParsing, out string effectivePath)
        {
            // The path for a reference might look like this for assembly foo.dll:
            // foo.dll
            // sub\foo.dll
            // {FrameworkName}{Version}\foo.dll
            // {FrameworkName}{Version}\sub1\foo.dll
            // {FrameworkName}{Version}\sub1\sub2\foo.dll

            // Get the target framework string if specified
            string targetFrameworkString = Path.GetDirectoryName(path).Split(Path.DirectorySeparatorChar).First();

            effectivePath = path;

            if (String.IsNullOrEmpty(targetFrameworkString))
            {
                return null;
            }

            var targetFramework = ParseFrameworkName(targetFrameworkString);
            if (strictParsing || targetFramework != UnsupportedFrameworkName)
            {
                // skip past the framework folder and the character \
                effectivePath = path.Substring(targetFrameworkString.Length + 1);
                return targetFramework;
            }

            return null;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static bool TryGetCompatibleItems<T>(FrameworkName projectFramework, IEnumerable<T> items, out IEnumerable<T> compatibleItems) where T : IFrameworkTargetable
        {
            if (!items.Any())
            {
                compatibleItems = Enumerable.Empty<T>();
                return true;
            }

            // Not all projects have a framework, we need to consider those projects.
            var internalProjectFramework = projectFramework ?? EmptyFramework;

            // Turn something that looks like this:
            // item -> [Framework1, Framework2, Framework3] into
            // [{item, Framework1}, {item, Framework2}, {item, Framework3}]
            var normalizedItems = from item in items
                                  let frameworks = (item.SupportedFrameworks != null && item.SupportedFrameworks.Any()) ? item.SupportedFrameworks : new FrameworkName[] { null }
                                  from framework in frameworks
                                  select new
                                  {
                                      Item = item,
                                      TargetFramework = framework
                                  };

            // Group references by target framework (if there is no target framework we assume it is the default)
            var frameworkGroups = normalizedItems
                .GroupBy(g => g.TargetFramework, g => g.Item)
                .ToList();

            // Try to find the best match using NuGet's Get Nearest algorithm
            var nuGetFrameworkToFrameworkGroup = frameworkGroups
                .Where(g => g.Key != null)
                .ToDictionary(g => GetNuGetFramework(
                    NetPortableProfileTable.Instance,
                    ReferenceAssemblyFrameworkNameProvider.Instance,
                    g.Key),
                NuGetFramework.Comparer);

            var reducer = new FrameworkReducer(
                ReferenceAssemblyFrameworkNameProvider.Instance,
                ReferenceAssemblyCompatibilityProvider.Instance);

            var nearest = reducer.GetNearest(
                GetNuGetFramework(
                    NetPortableProfileTable.Instance,
                    ReferenceAssemblyFrameworkNameProvider.Instance,
                    internalProjectFramework),
                nuGetFrameworkToFrameworkGroup.Keys);
            
            if (nearest != null)
            {
                compatibleItems = nuGetFrameworkToFrameworkGroup[nearest];
            }
            else
            {
                compatibleItems = null;
            }
            
            bool hasItems = compatibleItems != null && compatibleItems.Any();
            if (!hasItems)
            {
                // if there's no matching profile, fall back to the items without target framework
                // because those are considered to be compatible with any target framework
                compatibleItems = frameworkGroups.Where(g => g.Key == null).SelectMany(g => g);
                hasItems = compatibleItems != null && compatibleItems.Any();
            }

            if (!hasItems)
            {
                compatibleItems = null;
            }

            return hasItems;
        }

        /// <summary>
        /// Returns all possible versions for a version. i.e. 1.0 would return 1.0, 1.0.0, 1.0.0.0
        /// </summary>
        internal static IEnumerable<SemanticVersion> GetPossibleVersions(SemanticVersion semVer)
        {
            // Trim the version so things like 1.0.0.0 end up being 1.0
            Version version = TrimVersion(semVer.Version);

            yield return new SemanticVersion(version, semVer.SpecialVersion);

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

        public static bool IsCompatible(FrameworkName projectFrameworkName, IEnumerable<FrameworkName> packageSupportedFrameworks)
        {
            if (packageSupportedFrameworks.Any())
            {
                return packageSupportedFrameworks.Any(packageSupportedFramework => IsCompatible(projectFrameworkName, packageSupportedFramework));
            }

            // No supported frameworks means that everything is supported.
            return true;
        }

        /// <summary>
        /// Determines if a package's target framework can be installed into a project's framework.
        /// </summary>
        /// <param name="projectFrameworkName">The project's framework</param>
        /// <param name="packageTargetFrameworkName">The package's target framework</param>
        internal static bool IsCompatible(FrameworkName projectFrameworkName, FrameworkName packageTargetFrameworkName)
        {
            return IsCompatible(
                NetPortableProfileTable.Instance,
                ReferenceAssemblyCompatibilityProvider.Instance,
                ReferenceAssemblyFrameworkNameProvider.Instance,
                projectFrameworkName,
                packageTargetFrameworkName);
        }

        internal static bool IsCompatible(
            NetPortableProfileTable table,
            IFrameworkCompatibilityProvider compatibilityProvider,
            IFrameworkNameProvider nameProvider,
            FrameworkName projectFrameworkName,
            FrameworkName packageTargetFrameworkName)
        {
            if (projectFrameworkName == null)
            {
                return true;
            }

            var projectNuGetFramework = GetNuGetFramework(
                table,
                nameProvider,
                projectFrameworkName);

            var packageTargetNuGetFramework = GetNuGetFramework(
                table,
                nameProvider,
                packageTargetFrameworkName);

            var isCompatible = compatibilityProvider.IsCompatible(
                projectNuGetFramework,
                packageTargetNuGetFramework);

            // Fallback to legacy portable compatibility logic if both:
            //   a) the modern compatibility code returns false
            //   b) the package framework is portable
            if (!isCompatible && packageTargetFrameworkName.IsPortableFramework())
            {
                return IsPortableLibraryCompatible(table, projectFrameworkName, packageTargetFrameworkName);
            }

            return isCompatible;
        }

        private static bool IsPortableLibraryCompatible(
            NetPortableProfileTable table,
            FrameworkName projectFrameworkName,
            FrameworkName packageTargetFrameworkName)
        {
            if (string.IsNullOrEmpty(packageTargetFrameworkName.Profile))
            {
                return false;
            }

            NetPortableProfile targetFrameworkPortableProfile = NetPortableProfile.Parse(table, packageTargetFrameworkName.Profile);
            if (targetFrameworkPortableProfile == null)
            {
                return false;
            }

            if (projectFrameworkName.IsPortableFramework())
            {
                // this is the case with Portable Library vs. Portable Library
                if (string.Equals(projectFrameworkName.Profile, packageTargetFrameworkName.Profile, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                NetPortableProfile frameworkPortableProfile = NetPortableProfile.Parse(table, projectFrameworkName.Profile);
                if (frameworkPortableProfile == null)
                {
                    return false;
                }

                return targetFrameworkPortableProfile.IsCompatibleWith(frameworkPortableProfile);
            }
            else
            {
                // this is the case with Portable Library installed into a normal project
                return targetFrameworkPortableProfile.IsCompatibleWith(table, projectFrameworkName);
            }
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

        public static bool IsPortableFramework(this FrameworkName framework)
        {
            // The profile part has been verified in the ParseFrameworkName() method.
            // By the time it is called here, it's guaranteed to be valid.
            // Thus we can ignore the profile part here
            return framework != null && PortableFrameworkIdentifier.Equals(framework.Identifier, StringComparison.OrdinalIgnoreCase);
        }

        internal static NuGetFramework GetNuGetFramework(
            NetPortableProfileTable table,
            IFrameworkNameProvider provider,
            FrameworkName framework)
        {
            // Use the short folder name as the common format between FrameworkName and
            // NuGetFramework. With portable frameworks, there are differences in
            // FrameworkName and NuGetFramework.DotNetFrameworkName.
            var folderName = GetShortFrameworkName(table, framework);
            return NuGetFramework.ParseFolder(folderName, provider);
        }
    }
}
