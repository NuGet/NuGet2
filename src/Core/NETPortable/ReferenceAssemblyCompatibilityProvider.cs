using NuGet.Frameworks;

namespace NuGet
{
    internal class ReferenceAssemblyCompatibilityProvider : CompatibilityProvider
    {

        public ReferenceAssemblyCompatibilityProvider(NetPortableProfileCollection profileCollection)
            : base(new ReferenceAssemblyFrameworkNameProvider(profileCollection))
        {
        }

        private static ReferenceAssemblyCompatibilityProvider _instance;

        public static ReferenceAssemblyCompatibilityProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ReferenceAssemblyCompatibilityProvider(
                        NetPortableProfileTable.Instance.Profiles);
                }

                return _instance;
            }
        }
    }
}
