using System;
using System.Globalization;
using System.IO;
using System.Net;
using NuGet.Resources;

namespace NuGet {
    public class PackageDownloader : IProgressProvider {
        private const string UserAgent = "Package-Installer/{0} ({1})";
        private readonly IHttpClient _httpClient;
        private readonly IPackageFactory _packageFactory;
        private readonly IHashProvider _hashProvider;

        public event EventHandler<ProgressEventArgs> ProgressAvailable;

        public PackageDownloader()
            : this(new HttpClient()) {
        }

        public PackageDownloader(IHttpClient httpClient)
            : this(httpClient, new ZipPackageFactory(), new CryptoHashProvider()) {
        }

        public PackageDownloader(IHttpClient httpClient, IPackageFactory packageFactory, IHashProvider hashProvider) {
            if (httpClient == null) {
                throw new ArgumentNullException("httpClient");
            }

            if (packageFactory == null) {
                throw new ArgumentNullException("packageFactory");
            }

            if (hashProvider == null) {
                throw new ArgumentNullException("hashProvider");
            }

            _httpClient = httpClient;
            _packageFactory = packageFactory;
            _hashProvider = hashProvider;
            var version = typeof(PackageDownloader).Assembly.GetNameSafe().Version;
            _httpClient.UserAgent = String.Format(CultureInfo.InvariantCulture, UserAgent, version, Environment.OSVersion);
        }

        public IPackage DownloadPackage(Uri uri, byte[] packageHash, IPackageMetadata package) {
            if (uri == null) {
                throw new ArgumentNullException("uri");
            }

            if (packageHash == null) {
                throw new ArgumentNullException("packageHash");
            }

            if (package == null) {
                throw new ArgumentNullException("package");
            }

            // Get the operation display text
            string operation = String.Format(CultureInfo.CurrentCulture, NuGetResources.DownloadProgressStatus, package.Id, package.Version);

            EventHandler<ProgressEventArgs> handler = (sender, e) => {
                OnPackageDownloadProgress(new ProgressEventArgs(operation, e.PercentComplete));
            };

            try {
                _httpClient.ProgressAvailable += handler;

                // TODO: This gets held onto in memory which we want to get rid of eventually
                byte[] buffer = _httpClient.DownloadData(uri);

                if (!_hashProvider.VerifyHash(buffer, packageHash)) {
                    throw new InvalidDataException(NuGetResources.PackageContentsVerifyError);
                }

                return _packageFactory.CreatePackage(() => {
                    return new MemoryStream(buffer);
                });
            }
            finally {
                _httpClient.ProgressAvailable -= handler;
            }
        }

        private void OnPackageDownloadProgress(ProgressEventArgs e) {
            if (ProgressAvailable != null) {
                ProgressAvailable(this, e);
            }
        }
    }
}