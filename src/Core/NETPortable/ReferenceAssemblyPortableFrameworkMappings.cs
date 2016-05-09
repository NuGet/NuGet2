using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Frameworks;

namespace NuGet
{
    internal class ReferenceAssemblyPortableFrameworkMappings : IPortableFrameworkMappings
    {
        public ReferenceAssemblyPortableFrameworkMappings(NetPortableProfileCollection profileCollection)
        {
            var table = new NetPortableProfileTable(profileCollection);
            foreach (var profile in profileCollection)
            {
                AddPortableProfile(table, profile);
            }
        }

        private void AddPortableProfile(NetPortableProfileTable table, NetPortableProfile profile)
        {
            if (!profile.Name.StartsWith(NetPortableProfile.ProfilePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var rawNumber = profile.Name.Substring(NetPortableProfile.ProfilePrefix.Length);
            int number;
            if (!int.TryParse(rawNumber, out number))
            {
                return;
            }

            var profileFrameworks = new KeyValuePair<int, NuGetFramework[]>(
                number,
                profile.SupportedFrameworks.Select(f => VersionUtility.GetNuGetFramework(
                        table,
                        DefaultFrameworkNameProvider.Instance,
                        f)).ToArray());
            _profileFrameworks.Add(profileFrameworks);

            var profileOptionalFrameworks = new KeyValuePair<int, NuGetFramework[]>(
                number,
                profile.OptionalFrameworks.Select(f => VersionUtility.GetNuGetFramework(
                    table,
                    DefaultFrameworkNameProvider.Instance, 
                    f)).ToArray());
            _profileOptionalFrameworks.Add(profileOptionalFrameworks);
        }
        
        /// <summary>
        /// Use the compatibility mappings from NuGet 3.x, since these are not all all expressed
        /// in the old world.
        /// </summary>
        public IEnumerable<KeyValuePair<int, FrameworkRange>> CompatibilityMappings
        {
            get { return DefaultPortableFrameworkMappings.Instance.CompatibilityMappings; }
        }

        private readonly List<KeyValuePair<int, NuGetFramework[]>> _profileFrameworks
            = new List<KeyValuePair<int, NuGetFramework[]>>();

        public IEnumerable<KeyValuePair<int, NuGetFramework[]>> ProfileFrameworks
        {
            get { return _profileFrameworks; }
        }

        private readonly List<KeyValuePair<int, NuGetFramework[]>> _profileOptionalFrameworks
            = new List<KeyValuePair<int, NuGetFramework[]>>();

        public IEnumerable<KeyValuePair<int, NuGetFramework[]>> ProfileOptionalFrameworks
        {
            get { return _profileOptionalFrameworks; }
        }

        private static ReferenceAssemblyPortableFrameworkMappings _instance;

        public static ReferenceAssemblyPortableFrameworkMappings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ReferenceAssemblyPortableFrameworkMappings(
                        NetPortableProfileTable.Instance.Profiles);
                }

                return _instance;
            }
        }
    }
}
