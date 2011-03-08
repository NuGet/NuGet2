using System;
using System.Globalization;
using NuGet.Resources;

namespace NuGet {
    public class PackageRepositoryFactory : IPackageRepositoryFactory {
        private static readonly PackageRepositoryFactory _default = new PackageRepositoryFactory();
        private readonly IHttpClient _httpClient;

        public PackageRepositoryFactory() :
            this(new HttpClient()) {
        }

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

            try {
                uri = _httpClient.GetRedirectedUri(uri);
            }
            catch (Exception exception) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.UnavailablePackageSource, packageSource),
                    exception);
            }

            // Make sure we get resolve any fwlinks before creating the repository
            return new DataServicePackageRepository(uri, _httpClient);
        }
    }
}
