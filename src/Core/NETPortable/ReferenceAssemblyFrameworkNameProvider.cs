using System.Collections.Generic;
using NuGet.Frameworks;

namespace NuGet
{
    internal class ReferenceAssemblyFrameworkNameProvider : FrameworkNameProvider
    {
        public ReferenceAssemblyFrameworkNameProvider(NetPortableProfileCollection profileCollection)
            : base(GetMappings(), GetPortableMappings(profileCollection))
        {
        }

        private static IEnumerable<IFrameworkMappings> GetMappings()
        {
            yield return DefaultFrameworkMappings.Instance;
        }

        private static IEnumerable<IPortableFrameworkMappings> GetPortableMappings(NetPortableProfileCollection profileCollection)
        {
            yield return new ReferenceAssemblyPortableFrameworkMappings(profileCollection);
        }

        private static ReferenceAssemblyFrameworkNameProvider _instance;

        public static ReferenceAssemblyFrameworkNameProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ReferenceAssemblyFrameworkNameProvider(
                        NetPortableProfileTable.Instance.Profiles);
                }

                return _instance;
            }
        }
    }
}
