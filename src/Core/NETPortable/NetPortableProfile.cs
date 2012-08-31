using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Resources;

namespace NuGet
{
    /// <summary>
    /// Represent one profile of the .NET Portable library
    /// </summary>
    public class NetPortableProfile : IEquatable<NetPortableProfile>
    {
        private string _customProfile;

        public NetPortableProfile(string name, IEnumerable<FrameworkName> supportedFrameworks)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "name");
            }

            if (supportedFrameworks == null)
            {
                throw new ArgumentNullException("supportedFrameworks");
            }

            var frameworks = supportedFrameworks.ToList();
            if (frameworks.Any(f => f == null))
            {
                throw new ArgumentException(NuGetResources.SupportedFrameworkIsNull, "supportedFrameworks");
            }

            if (frameworks.Count == 0)
            {
                throw new ArgumentOutOfRangeException("supportedFrameworks");
            }

            Name = name;
            SupportedFrameworks = new ReadOnlyHashSet<FrameworkName>(frameworks);
        }

        public string Name { get; private set; }

        public ISet<FrameworkName> SupportedFrameworks { get; private set; }

        public bool Equals(NetPortableProfile other)
        {
            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                   SupportedFrameworks.SetEquals(other.SupportedFrameworks);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() * 3137 + SupportedFrameworks.GetHashCode();
        }

        /// <summary>
        /// Returns the string that represents all supported frameworks by this profile, separated by the + sign.
        /// </summary>
        /// <example>
        /// sl4+net45+windows8
        /// </example>
        public string CustomProfileString
        {
            get
            {
                if (_customProfile == null)
                {
                    _customProfile = String.Join("+", SupportedFrameworks.Select(f => VersionUtility.GetShortFrameworkName(f)));
                }

                return _customProfile;
            }
        }

        public bool IsCompatibleWith(NetPortableProfile other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            return other.SupportedFrameworks.All(
                projectFramework => this.SupportedFrameworks.Any(
                    packageFramework => VersionUtility.IsCompatible(projectFramework, packageFramework)));
        }

        public bool IsCompatibleWith(FrameworkName framework)
        {
            if (framework == null)
            {
                throw new ArgumentNullException("framework");
            }

            return SupportedFrameworks.Any(f => VersionUtility.IsCompatible(framework, f));
        }

        /// <summary>
        /// Attempt to parse a profile string into an instance of <see cref="NetPortableProfile"/>.
        /// The profile string can be either ProfileXXX or sl4+net45+wp7
        /// </summary>
        public static NetPortableProfile Parse(string profileValue)
        {
            if (String.IsNullOrEmpty(profileValue))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "profileValue");
            }

            if (profileValue.StartsWith("Profile", StringComparison.OrdinalIgnoreCase))
            {
                return NetPortableProfileTable.GetProfile(profileValue);
            }

            VersionUtility.ValidatePortableFrameworkProfilePart(profileValue);

            var supportedFrameworks = profileValue.Split(new [] {'+'}, StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(VersionUtility.ParseFrameworkName);
            return new NetPortableProfile(profileValue, supportedFrameworks);
        }
    }
}