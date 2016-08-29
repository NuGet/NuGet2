using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NuGet.Resources;

namespace NuGet
{
    /// <summary>
    /// A hybrid implementation of SemVer that supports semantic versioning as described at http://semver.org while not strictly enforcing it to 
    /// allow older 4-digit versioning schemes to continue working.
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(SemanticVersionTypeConverter))]
    public sealed class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        private const RegexOptions _flags = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

        // Versions containing up to 4 digits
        private static readonly Regex _semanticVersionRegex = new Regex(@"^(?<Version>\d+(\s*\.\s*\d+){0,3})(?<Release>-([0]\b|[0]$|[0][0-9]*[A-Za-z-]+|[1-9A-Za-z-][0-9A-Za-z-]*)+(\.([0]\b|[0]$|[0][0-9]*[A-Za-z-]+|[1-9A-Za-z-][0-9A-Za-z-]*)+)*)?(?<Metadata>\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$", _flags);

        // Strict SemVer 2.0.0 format, this may contain only 3 digits.
        private static readonly Regex _strictSemanticVersionRegex = new Regex(@"^(?<Version>([0-9]|[1-9][0-9]*)(\.([0-9]|[1-9][0-9]*)){2})(?<Release>-([0]\b|[0]$|[0][0-9]*[A-Za-z-]+|[1-9A-Za-z-][0-9A-Za-z-]*)+(\.([0]\b|[0]$|[0][0-9]*[A-Za-z-]+|[1-9A-Za-z-][0-9A-Za-z-]*)+)*)?(?<Metadata>\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$", _flags);

        private readonly string _originalString;
        private string _normalizedVersionString;

        public SemanticVersion(string version)
            : this(Parse(version))
        {
            // The constructor normalizes the version string so that it we do not need to normalize it every time we need to operate on it. 
            // The original string represents the original form in which the version is represented to be used when printing.
            _originalString = version;
        }

        public SemanticVersion(int major, int minor, int build, int revision)
            : this(new Version(major, minor, build, revision))
        {
        }

        public SemanticVersion(int major, int minor, int build, string specialVersion)
            : this(new Version(major, minor, build), specialVersion)
        {
        }

        public SemanticVersion(int major, int minor, int build, string specialVersion, string metadata)
            : this(new Version(major, minor, build), specialVersion, metadata)
        {
        }

        public SemanticVersion(Version version)
            : this(version, String.Empty)
        {
        }

        public SemanticVersion(Version version, string specialVersion)
            : this(version, specialVersion, metadata: null, originalString: null)
        {
        }

        public SemanticVersion(Version version, string specialVersion, string metadata)
         : this(version, specialVersion, metadata, originalString: null)
        {
        }

        private SemanticVersion(Version version, string specialVersion, string metadata, string originalString)
        {
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            Version = NormalizeVersionValue(version);
            SpecialVersion = specialVersion ?? String.Empty;
            Metadata = metadata;

            _originalString = String.IsNullOrEmpty(originalString) ? version.ToString() 
                + (!String.IsNullOrEmpty(specialVersion) ? '-' + specialVersion : null)
                + (!String.IsNullOrEmpty(metadata) ? '+' + metadata : null)
                : originalString;
        }

        internal SemanticVersion(SemanticVersion semVer)
        {
            _originalString = semVer.ToOriginalString();
            Version = semVer.Version;
            SpecialVersion = semVer.SpecialVersion;
            Metadata = semVer.Metadata;
        }

        /// <summary>
        /// Gets the normalized version portion.
        /// </summary>
        public Version Version
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the optional special version.
        /// </summary>
        public string SpecialVersion
        {
            get;
            private set;
        }

        /// <summary>
        /// SemVer 2.0.0 metadata. This is not used for comparing or sorting.
        /// </summary>
        public string Metadata
        {
            get;
            private set;
        }

        public string[] GetOriginalVersionComponents()
        {
            if (!String.IsNullOrEmpty(_originalString))
            {
                string original;

                // search the start of the SpecialVersion part or metadata, if any
                int labelIndex = _originalString.IndexOfAny(new char[] { '-', '+' });
                if (labelIndex != -1)
                {
                    // remove the SpecialVersion or metadata part
                    original = _originalString.Substring(0, labelIndex);
                }
                else
                {
                    original = _originalString;
                }

                return SplitAndPadVersionString(original);
            }
            else
            {
                return SplitAndPadVersionString(Version.ToString());
            }
        }

        private static string[] SplitAndPadVersionString(string version)
        {
            string[] a = version.Split('.');
            if (a.Length == 4)
            {
                return a;
            }
            else
            {
                // if 'a' has less than 4 elements, we pad the '0' at the end 
                // to make it 4.
                var b = new string[4] { "0", "0", "0", "0" };
                Array.Copy(a, 0, b, 0, a.Length);
                return b;
            }
        }

        /// <summary>
        /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
        /// </summary>
        public static SemanticVersion Parse(string version)
        {
            if (String.IsNullOrEmpty(version))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "version");
            }

            SemanticVersion semVer;
            if (!TryParse(version, out semVer))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, NuGetResources.InvalidVersionString, version), "version");
            }
            return semVer;
        }

        /// <summary>
        /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
        /// </summary>
        public static bool TryParse(string version, out SemanticVersion value)
        {
            return TryParseInternal(version, _semanticVersionRegex, out value);
        }

        /// <summary>
        /// Parses a version string using strict semantic versioning rules that allows exactly 3 components and an optional special version.
        /// </summary>
        public static bool TryParseStrict(string version, out SemanticVersion value)
        {
            return TryParseInternal(version, _strictSemanticVersionRegex, out value);
        }

        private static bool TryParseInternal(string version, Regex regex, out SemanticVersion semVer)
        {
            semVer = null;
            if (String.IsNullOrEmpty(version))
            {
                return false;
            }

            var match = regex.Match(version.Trim());
            Version versionValue;
            if (!match.Success || !Version.TryParse(match.Groups["Version"].Value, out versionValue))
            {
                return false;
            }

            semVer = new SemanticVersion(
                NormalizeVersionValue(versionValue),
                RemoveLeadingChar(match.Groups["Release"].Value),
                RemoveLeadingChar(match.Groups["Metadata"].Value),
                version.Replace(" ", ""));

            return true;
        }

        // Remove the - or + from a version section.
        private static string RemoveLeadingChar(string s)
        {
            if (s != null && s.Length > 0)
            {
                return s.Substring(1, s.Length - 1);
            }

            return s;
        }

        /// <summary>
        /// Attempts to parse the version token as a SemanticVersion.
        /// </summary>
        /// <returns>An instance of SemanticVersion if it parses correctly, null otherwise.</returns>
        public static SemanticVersion ParseOptionalVersion(string version)
        {
            SemanticVersion semVer;
            TryParse(version, out semVer);
            return semVer;
        }

        private static Version NormalizeVersionValue(Version version)
        {
            return new Version(version.Major,
                               version.Minor,
                               Math.Max(version.Build, 0),
                               Math.Max(version.Revision, 0));
        }

        public int CompareTo(object obj)
        {
            if (Object.ReferenceEquals(obj, null))
            {
                return 1;
            }
            SemanticVersion other = obj as SemanticVersion;
            if (other == null)
            {
                throw new ArgumentException(NuGetResources.TypeMustBeASemanticVersion, "obj");
            }
            return CompareTo(other);
        }

        public int CompareTo(SemanticVersion other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return 1;
            }

            int result = Version.CompareTo(other.Version);

            if (result != 0)
            {
                return result;
            }

            bool empty = String.IsNullOrEmpty(SpecialVersion);
            bool otherEmpty = String.IsNullOrEmpty(other.SpecialVersion);
            if (empty && otherEmpty)
            {
                return 0;
            }
            else if (empty)
            {
                return 1;
            }
            else if (otherEmpty)
            {
                return -1;
            }

            // Compare the release labels using SemVer 2.0.0 comparision rules.
            var releaseLabels = SpecialVersion.Split('.');
            var otherReleaseLabels = other.SpecialVersion.Split('.');

            return CompareReleaseLabels(releaseLabels, otherReleaseLabels);
        }

        public static bool operator ==(SemanticVersion version1, SemanticVersion version2)
        {
            if (Object.ReferenceEquals(version1, null))
            {
                return Object.ReferenceEquals(version2, null);
            }
            return version1.Equals(version2);
        }

        public static bool operator !=(SemanticVersion version1, SemanticVersion version2)
        {
            return !(version1 == version2);
        }

        public static bool operator <(SemanticVersion version1, SemanticVersion version2)
        {
            if (version1 == null)
            {
                throw new ArgumentNullException("version1");
            }
            return version1.CompareTo(version2) < 0;
        }

        public static bool operator <=(SemanticVersion version1, SemanticVersion version2)
        {
            return (version1 == version2) || (version1 < version2);
        }

        public static bool operator >(SemanticVersion version1, SemanticVersion version2)
        {
            if (version1 == null)
            {
                throw new ArgumentNullException("version1");
            }
            return version2 < version1;
        }

        public static bool operator >=(SemanticVersion version1, SemanticVersion version2)
        {
            return (version1 == version2) || (version1 > version2);
        }

        /// <summary>
        /// Returns the original version string without metadata.
        /// </summary>
        public override string ToString()
        {
            if (IsSemVer2())
            {
                // Normalize semver2 to match NuGet.Versioning
                return ToNormalizedString();
            }
            else
            {
                // Remove metadata from the original string if it exists.
                var plusIndex = _originalString.IndexOf('+');

                if (plusIndex > -1)
                {
                    return _originalString.Substring(0, plusIndex);
                }
            }

            return _originalString;
        }

        /// <summary>
        /// Returns the normalized string representation of this instance of <see cref="SemanticVersion"/>.
        /// If the instance can be strictly parsed as a <see cref="SemanticVersion"/>, the normalized version
        /// string if of the format {major}.{minor}.{build}[-{special-version}]. If the instance has a non-zero
        /// value for <see cref="Version.Revision"/>, the format is {major}.{minor}.{build}.{revision}[-{special-version}].
        /// </summary>
        /// <remarks>Metadata is not included.</remarks>
        /// <returns>The normalized string representation.</returns>
        public string ToNormalizedString()
        {
            if (_normalizedVersionString == null)
            {
                var builder = new StringBuilder();
                builder
                    .Append(Version.Major)
                    .Append('.')
                    .Append(Version.Minor)
                    .Append('.')
                    .Append(Math.Max(0, Version.Build));

                if (Version.Revision > 0)
                {
                    builder.Append('.')
                           .Append(Version.Revision);
                }

                if (!string.IsNullOrEmpty(SpecialVersion))
                {
                    builder.Append('-')
                           .Append(SpecialVersion);
                }

                _normalizedVersionString = builder.ToString();
            }

            return _normalizedVersionString;
        }

        /// <summary>
        /// Returns the full string including metadata.
        /// </summary>
        public string ToFullString()
        {
            var s = ToNormalizedString();

            if (!string.IsNullOrEmpty(Metadata))
            {
                s = string.Format(CultureInfo.InvariantCulture, "{0}+{1}", s, Metadata);
            }

            return s;
        }

        /// <summary>
        /// Returns the original string used to construct the version. This includes metadata.
        /// </summary>
        public string ToOriginalString()
        {
            return _originalString;
        }

        /// <summary>
        /// True if the version contains metadata or multiple release labels.
        /// </summary>
        public bool IsSemVer2()
        {
            return !string.IsNullOrEmpty(Metadata) 
                || (!string.IsNullOrEmpty(SpecialVersion) && SpecialVersion.Contains("."));
        }

        public bool Equals(SemanticVersion other)
        {
            return !Object.ReferenceEquals(null, other) &&
                   Version.Equals(other.Version) &&
                   SpecialVersion.Equals(other.SpecialVersion, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            SemanticVersion semVer = obj as SemanticVersion;
            return !Object.ReferenceEquals(null, semVer) && Equals(semVer);
        }

        public override int GetHashCode()
        {
            int hashCode = Version.GetHashCode();
            if (SpecialVersion != null)
            {
                hashCode = hashCode * 4567 + SpecialVersion.GetHashCode();
            }

            return hashCode;
        }

        /// <summary>
        /// Compares sets of release labels.
        /// </summary>
        private static int CompareReleaseLabels(IEnumerable<string> version1, IEnumerable<string> version2)
        {
            var result = 0;

            var a = version1.GetEnumerator();
            var b = version2.GetEnumerator();

            var aExists = a.MoveNext();
            var bExists = b.MoveNext();

            while (aExists || bExists)
            {
                if (!aExists && bExists)
                {
                    return -1;
                }

                if (aExists && !bExists)
                {
                    return 1;
                }

                // compare the labels
                result = CompareRelease(a.Current, b.Current);

                if (result != 0)
                {
                    return result;
                }

                aExists = a.MoveNext();
                bExists = b.MoveNext();
            }

            return result;
        }

        /// <summary>
        /// Release labels are compared as numbers if they are numeric, otherwise they will be compared
        /// as strings.
        /// </summary>
        private static int CompareRelease(string version1, string version2)
        {
            var version1Num = 0;
            var version2Num = 0;
            var result = 0;

            // check if the identifiers are numeric
            var v1IsNumeric = Int32.TryParse(version1, out version1Num);
            var v2IsNumeric = Int32.TryParse(version2, out version2Num);

            // if both are numeric compare them as numbers
            if (v1IsNumeric && v2IsNumeric)
            {
                result = version1Num.CompareTo(version2Num);
            }
            else if (v1IsNumeric || v2IsNumeric)
            {
                // numeric labels come before alpha labels
                if (v1IsNumeric)
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
            }
            else
            {
                // Ignoring 2.0.0 case sensitive compare. Everything will be compared case insensitively as 2.0.1 specifies.
                result = StringComparer.OrdinalIgnoreCase.Compare(version1, version2);
            }

            return result;
        }
    }
}
