using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using NuGet.Resources;

namespace NuGet {

    // REVIEW: This class isn't super clean. Maybe this object should be passed around instead
    // of being static
    public class PackageDownloader {
        private const string UserAgent = "Package-Installer/{0} ({1})";
        private IHttpClient _httpClient;
        private IPackageFactory _packageFactory = null;
        private IHashProvider _hashProvider = null;

        public PackageDownloader(IHttpClient httpClient)
            : this(httpClient, null, null) {
        }

        public PackageDownloader(IHttpClient httpClient, IPackageFactory packageFactory, IHashProvider hashProvider) {
            if (httpClient == null) {
                throw new ArgumentNullException("httpClient");
            }

            _httpClient = httpClient;
            _packageFactory = packageFactory ?? new ZipPackageFactory();
            _hashProvider = hashProvider;

            var version = typeof(PackageDownloader).Assembly.GetNameSafe().Version;
            string userAgent = String.Format(CultureInfo.InvariantCulture, UserAgent, version, Environment.OSVersion);
            httpClient.UserAgent = userAgent;
        }

        public IPackage DownloadPackage(Uri uri) {
            return DownloadPackage(uri, packageHash: null, useCache: true);
        }

        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "We can't dispose an object if we want to return it.")]
        public IPackage DownloadPackage(Uri uri, byte[] packageHash, bool useCache) {
            byte[] cachedBytes = null;

            return _packageFactory.CreatePackage(() => {
                if (useCache && cachedBytes != null) {
                    return new MemoryStream(cachedBytes);
                }

                using (Stream responseStream = GetResponseStream(uri)) {
                    // ZipPackages require a seekable stream
                    var memoryStream = new MemoryStream();
                    // Copy the stream
                    responseStream.CopyTo(memoryStream);

                    IHashProvider hashProvider = _hashProvider;
                    if (packageHash != null && hashProvider != null && !hashProvider.VerifyHash(memoryStream.ToArray(), packageHash)) {
                        throw new InvalidDataException(NuGetResources.PackageContentsVerifyError);
                    }

                    // Move it back to the beginning
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    if (useCache) {
                        // Cache the bytes for this package
                        cachedBytes = memoryStream.ToArray();
                    }

                    return memoryStream;
                }
            });
        }

        Stream GetResponseStream(Uri uri) {
            WebResponse response = GetResponse(uri);
            return response.GetResponseStream();
        }

        WebResponse GetResponse(Uri uri) {
            WebRequest request = _httpClient.CreateRequest(uri);
            return request.GetResponse();
        }

        public Uri GetRedirectedUri(Uri uri) {
            WebResponse response = GetResponse(uri);
            return response.ResponseUri;
        }

        public void InitializeRequest(WebRequest request) {
            _httpClient.InitializeRequest(request);
        }
    }
}
