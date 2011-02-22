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
        private IPackage _package;
        private IDisposable _subscribeDisposable;

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
            _subscribeDisposable = _httpClient.Subscribe(this);
            var version = typeof(PackageDownloader).Assembly.GetNameSafe().Version;
            string userAgent = String.Format(CultureInfo.InvariantCulture, UserAgent, version, Environment.OSVersion);
            _httpClient.UserAgent = userAgent;
        }

        public IPackage DownloadPackage(Uri uri, byte[] packageHash, IPackage package, IProgressReporter progressReporter) {

            // make sure no previous download is pending
            Debug.Assert(_progressReporter == null);

            _progressReporter = progressReporter ?? NullProgressReporter.Instance;
            _package = package;

            byte[] buffer = _httpClient.DownloadData(uri);

            if (!_hashProvider.VerifyHash(buffer, packageHash)) {
                throw new InvalidDataException(NuGetResources.PackageContentsVerifyError);
            }

            try {
                return _packageFactory.CreatePackage(() => new MemoryStream(buffer));
            }
            catch (AggregateException ex) {
                if (ex.InnerException != null) {
                    throw ex.InnerException;
                }

                throw;
            }
        }

        public void InitializeRequest(WebRequest request) {
            _httpClient.InitializeRequest(request);
        }

        public void OnCompleted() {
            _progressReporter = null;
            _subscribeDisposable.Dispose();
        }

        public void OnError(Exception error) {
            throw error;
        }

        public void OnNext(int value) {
            _progressReporter.ReportProgress(
                String.Format(CultureInfo.CurrentCulture, NuGetResources.DownloadProgressStatus, _package.Id, _package.Version),
                value);
        }
    }
}