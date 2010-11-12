using System;

namespace NuGet {
    public class PackageRepositoryFactory : IPackageRepositoryFactory {
        private static readonly PackageRepositoryFactory _default = new PackageRepositoryFactory();
        private IHttpClient _httpClient;

        public PackageRepositoryFactory() : this(new HttpClient()) { }

        public PackageRepositoryFactory(IHttpClient httpClient) {
            _httpClient = httpClient;
        }

        public static PackageRepositoryFactory Default {
            get {
                return _default;
            }
        }

        public virtual IPackageRepository CreateRepository(PackageSource packageSource) {
            if (packageSource == null) {
                throw new ArgumentNullException("packageSource");
            }

            if (packageSource.IsAggregate) {
                throw new NotSupportedException();
            }

            Uri uri = new Uri(packageSource.Source);
            if (uri.IsFile) {
                return new LocalPackageRepository(uri.LocalPath);
            }
            // Make sure we get resolve any fwlinks before creating the repository
            return new DataServicePackageRepository(_httpClient.GetRedirectedUri(uri), _httpClient);
        }
    }
}
