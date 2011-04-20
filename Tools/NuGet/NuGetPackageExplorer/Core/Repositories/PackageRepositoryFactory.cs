using System;

namespace NuGet {
    public class PackageRepositoryFactory {
        private static readonly PackageRepositoryFactory _default = new PackageRepositoryFactory();
        private const string _UserAgentClient = "NuGet Package Explorer";

        public static PackageRepositoryFactory Default {
            get {
                return _default;
            }
        }

        public virtual IPackageRepository CreateRepository(IHttpClient repositoryClient) {
            if (null == repositoryClient) {
                throw new ArgumentNullException("repositoryClient");
            }
            return new DataServicePackageRepository(repositoryClient);
        }
    }
}
