using System;
using System.Globalization;
using NuGet.Resources;
using NuGet.Repositories;

namespace NuGet {
    public class PackageRepositoryFactory {
        private static readonly PackageRepositoryFactory _default = new PackageRepositoryFactory();
        //private IHttpClient _httpClient;
        //private const string _UserAgentPattern = "NuGet Package Explorer/{0} ({1})";

        //public PackageRepositoryFactory() : this(new HttpClient()) { }

        //public PackageRepositoryFactory(IHttpClient httpClient) {
        //    _httpClient = httpClient;

        //    var version = typeof(GalleryServer).Assembly.GetNameSafe().Version;
        //    var userAgent = String.Format(CultureInfo.InvariantCulture, _UserAgentPattern, version, Environment.OSVersion);
        //    httpClient.UserAgent = userAgent;
        //}

        public static PackageRepositoryFactory Default {
            get {
                return _default;
            }
        }

        public virtual IPackageRepository CreateRepository(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            Uri uri = new Uri(source);
            IHttpClient client = HttpClientFactory.Default.CreateClient(uri);
            try
            {
                client = client.GetRedirectedClient(uri);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    null, source),
                    exception);
            }

            // Make sure we get resolve any fwlinks before creating the repository
            return new DataServicePackageRepository(client);
        }
    }
}
