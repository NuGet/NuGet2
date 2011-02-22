using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using NuGet.Resources;

namespace NuGet {

    public class PackageDownloader : IObserver<int> {
        private const string UserAgent = "Package-Installer/{0} ({1})";
        private IHttpClient _httpClient;
        private IPackageFactory _packageFactory = null;
        private IHashProvider _hashProvider = null;
        private IProgressReporter _progressReporter;
        private string _currentOperation;

        public PackageDownloader()
            : this(null) {
        }

        public PackageDownloader(IHttpClient httpClient)
            : this(httpClient, null, null) {
        }

        public PackageDownloader(IHttpClient httpClient, IPackageFactory packageFactory, IHashProvider hashProvider) {
            _httpClient = httpClient ?? new HttpClient();
            _packageFactory = packageFactory ?? new ZipPackageFactory();
            _hashProvider = hashProvider ?? new CryptoHashProvider();
            _httpClient.Subscribe(this);
            var version = typeof(PackageDownloader).Assembly.GetNameSafe().Version;
            string userAgent = String.Format(CultureInfo.InvariantCulture, UserAgent, version, Environment.OSVersion);
            _httpClient.UserAgent = userAgent;
        }

        public IPackage DownloadPackage(Uri uri, byte[] packageHash, IPackageMetadata package, IProgressReporter progressReporter) {
            if (package == null) {
                throw new ArgumentNullException("package");
            }
            _progressReporter = progressReporter ?? NullProgressReporter.Instance;
            _currentOperation = String.Format(CultureInfo.CurrentCulture, NuGetResources.DownloadProgressStatus, package.Id, package.Version);

            byte[] buffer = _httpClient.DownloadData(uri);
            if (!_hashProvider.VerifyHash(buffer, packageHash)) {
                throw new InvalidDataException(NuGetResources.PackageContentsVerifyError);
            }

            return _packageFactory.CreatePackage(() => new MemoryStream(buffer));
        }

        public void InitializeRequest(WebRequest request) {
            _httpClient.InitializeRequest(request);
        }

        public void OnCompleted() {
            _progressReporter = null;
        }

        public void OnError(Exception error) {
        }

        public void OnNext(int value) {
            _progressReporter.ReportProgress(_currentOperation, value);
        }
    }
}