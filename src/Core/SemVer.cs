using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Internal.Web.Utils;
using NuGet.Resources;

namespace NuGet {
    /// <summary>
    /// A hybrid implementation of SemVer that supports semantic versioning as described at http://semver.org while not strictly enforcing it to 
    /// allow older 4-digit versioning schemes to continue working.
    /// </summary>
    public sealed class SemVer : IComparable, IComparable<SemVer>, IEquatable<SemVer> {
        private readonly string _originalString; 

        public SemVer(string version)
            : this(Parse(version)) {
            _originalString = version;
        }

        public SemVer(int major, int minor, int build, int revision)
            : this(new Version(major, minor, build, revision)) {
        }

        public SemVer(int major, int minor, int build, int revision, string specialVersion)
            : this(new Version(major, minor, build, revision), specialVersion) {
        }

        public SemVer(Version version)
            : this(version, String.Empty) {
        }

        public SemVer(Version version, string specialVersion)
            : this(version, specialVersion, null) {
        }

        private SemVer(Version version, string specialVersion, string originalString) {
            if (version == null) {
                throw new ArgumentNullException("version");
            }
            Version = NormalizeVersionValue(version);
            SpecialVersion = specialVersion ?? String.Empty;
            _originalString = String.IsNullOrEmpty(originalString) ? version.ToString() + specialVersion : originalString;
        }

        internal SemVer(SemVer semVer) {
            _originalString = semVer.ToString();
            Version = semVer.Version;
            SpecialVersion = semVer.SpecialVersion;
        }

        public Version Version {
            get;
            private set;
        }

        public string SpecialVersion {
            get;
            private set;
        }

        public static SemVer Parse(string version) {
            if (String.IsNullOrEmpty(version)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "version");
            }

            SemVer semVer;
            if (!TryParse(version, out semVer)) {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, NuGetResources.InvalidVersionString, version), "version");
            }
            return semVer;
        }

        public static bool TryParse(string version, out SemVer value) {
            const string semVerRegex = @"^(?<Version>\d+(\s*\.\s*\d+){0,3})(?<Release>[a-z][0-9a-z-]*)?$";
            return TryParseInternal(version, semVerRegex, out value);
        }

        public static bool TryParseStrict(string version, out SemVer value) {
            const string strictSemVer = @"^(?<Version>\d+(\.\d+){2})(?<Release>[a-z][0-9a-z-]*)?$";
            return TryParseInternal(version, strictSemVer, out value);
        }

        private static bool TryParseInternal(string version, string regex, out SemVer semVer) {
            semVer = null;
            if (String.IsNullOrEmpty(version)) {
                return false;
            }
            var regexFlags = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;
            var semVerRegex = new Regex(regex, regexFlags);

            var match = semVerRegex.Match(version.Trim());
            Version versionValue;
            if (!match.Success || !Version.TryParse(match.Groups["Version"].Value, out versionValue)) {
                return false;
            }

            semVer = new SemVer(NormalizeVersionValue(versionValue), match.Groups["Release"].Value, version.Replace(" ", ""));
            return true;
        }

        public static SemVer ParseOptionalVersion(string version) {
            SemVer semVer;
            TryParse(version, out semVer);
            return semVer;
        }

        private static Version NormalizeVersionValue(Version version) {
            return new Version(version.Major,
                               version.Minor,
                               Math.Max(version.Build, 0),
                               Math.Max(version.Revision, 0));
        }

        public int CompareTo(object obj) {
            if (Object.ReferenceEquals(obj, null)) {
                return 1;
            }
            SemVer other = obj as SemVer;
            if (other == null) {
                throw new ArgumentException(NuGetResources.TypeMustBeASemVer, "obj");
            }
            return CompareTo(other);
        }

        public int CompareTo(SemVer other) {
            if (Object.ReferenceEquals(other, null)) {
                return 1;
            }

            int result = Version.CompareTo(other.Version);

            if (result != 0) {
                return result;
            }

            bool empty = String.IsNullOrEmpty(SpecialVersion);
            bool otherEmpty = String.IsNullOrEmpty(other.SpecialVersion);
            if (empty && otherEmpty) {
                return 0;
            }
            else if (empty) {
                return 1;
            }
            else if (otherEmpty) {
                return -1;
            }
            return StringComparer.OrdinalIgnoreCase.Compare(SpecialVersion, other.SpecialVersion);
        }

        public static bool operator ==(SemVer version1, SemVer version2) {
            if ((Object.ReferenceEquals(version1, null) || Object.ReferenceEquals(version2, null))) {
                return Object.ReferenceEquals(version1, null) && Object.ReferenceEquals(version2, null);
            }
            return version1.Equals(version2);
        }

        public static bool operator !=(SemVer version1, SemVer version2) {
            return !(version1 == version2);
        }

        public static bool operator <(SemVer version1, SemVer version2) {
            if (version1 == null) {
                throw new ArgumentNullException("version1");
            }
            return version1.CompareTo(version2) < 0;
        }

        public static bool operator <=(SemVer version1, SemVer version2) {
            return (version1 == version2) || (version1 < version2);
        }

        public static bool operator >(SemVer version1, SemVer version2) {
            if (version1 == null) {
                throw new ArgumentNullException("version1");
            }
            return version2 < version1;
        }

        public static bool operator >=(SemVer version1, SemVer version2) {
            return (version1 == version2) || (version1 > version2);
        }

        public override string ToString() {
            return _originalString;
        }

        public bool Equals(SemVer other) {
            return other != null && Version.Equals(other.Version) 
                                 && SpecialVersion.Equals(other.SpecialVersion, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) {
            SemVer semVer = obj as SemVer;
            return semVer != null && Equals(semVer);
        }

        public override int GetHashCode() {
            var hashCodeCombiner = new HashCodeCombiner();
            hashCodeCombiner.AddObject(Version);
            hashCodeCombiner.AddObject(SpecialVersion);

            return hashCodeCombiner.CombinedHash;
        }
    }
}
