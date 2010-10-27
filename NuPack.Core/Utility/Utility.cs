namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime.Versioning;
    using System.Text.RegularExpressions;

    internal static class Utility {
        private const string NetFrameworkIdentifier = ".NETFramework";

        private static readonly Dictionary<string, string> _knownIdentifiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "NET", NetFrameworkIdentifier },
            { ".NET", NetFrameworkIdentifier },
            { "NETFramework", NetFrameworkIdentifier },
            { ".NETFramework", NetFrameworkIdentifier },
            { "SL", "Silverlight" },
            { "Silverlight", "Silverlight" },
        };

        internal static Version ParseOptionalVersion(string versionString) {
            Version version;
            if (!String.IsNullOrEmpty(versionString) && Version.TryParse(versionString, out version)) {
                return version;
            }
            return null;
        }

        /// <summary>
        /// This function tries to normalize a string that represents framework version names into
        /// something a framework name that the package manager understands.
        /// </summary>
        internal static FrameworkName ParseFrameworkName(string frameworkName) {
            Debug.Assert(!String.IsNullOrEmpty(frameworkName));

            // {FrameworkName}{Version}
            var match = Regex.Match(frameworkName, @"\d+.*");
            int length = match.Success ? match.Index : frameworkName.Length;
            // Split the framework name into 2 parts, identifier and version
            string identifierPart = frameworkName.Substring(0, length);
            string versionPart = frameworkName.Substring(length);

            if (String.IsNullOrEmpty(identifierPart)) {
                // Use the default identifier (.NETFramework) if none specified
                identifierPart = NetFrameworkIdentifier;
            }
            else {
                // Try to nomalize the identifier to a known identifier
                string knownIdentifier;
                if (_knownIdentifiers.TryGetValue(identifierPart, out knownIdentifier)) {
                    identifierPart = knownIdentifier;
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
                version = GetDefaultTargetFrameworkVersion();
            }

            return new FrameworkName(identifierPart, version);
        }

        internal static Version GetDefaultTargetFrameworkVersion() {
            // We need to parse the version name out from the mscorlib's assembly name since
            // we can't call GetName() in medium trust
            string assemblyFullName = typeof(string).Assembly.FullName;
            return new AssemblyName(assemblyFullName).Version;
        }

        internal static bool IsManifest(string path) {
            return Path.GetExtension(path).Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase);
        }

        internal static FrameworkName GetDefaultTargetFramework() {
            return new FrameworkName(NetFrameworkIdentifier, GetDefaultTargetFrameworkVersion());
        }
    }
}
