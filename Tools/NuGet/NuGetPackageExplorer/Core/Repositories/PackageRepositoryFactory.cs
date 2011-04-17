using System;
using System.Globalization;
using NuGet.Resources;

namespace NuGet {
    public class PackageRepositoryFactory {
        private static readonly PackageRepositoryFactory _default = new PackageRepositoryFactory();
        private IHttpClient _httpClient;
        private const string _UserAgentClient = "NuGet Package Explorer";

        public PackageRepositoryFactory() : this(new HttpClient()) { }

        public PackageRepositoryFactory(IHttpClient httpClient) {
            _httpClient = httpClient;
            httpClient.UserAgent = HttpUtility.CreateUserAgentString(_UserAgentClient); ;
        }

        public static PackageRepositoryFactory Default {
            get {
                return _default;
            }
        }

        public virtual IPackageRepository CreateRepository(string source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            Uri uri = new Uri(source);
            try {
                uri = _httpClient.GetRedirectedUri(uri);
            }
            catch (Exception exception) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    null, source), 
                    exception);
            }

            // Make sure we get resolve any fwlinks before creating the repository
            return new DataServicePackageRepository(uri, _httpClient);
        }
    }
}
