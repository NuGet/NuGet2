using System;
using System.Globalization;
using System.IO;
using System.Net;
using NuGet.Resources;

namespace NuGet
{
    public class PackageDownloader : IHttpClientEvents
    {
        private const string DefaultUserAgentClient = "NuGet Visual Studio Extension";
        private readonly IPackageFactory _packageFactory;
        private readonly IHashProvider _hashProvider;

        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };
        public event EventHandler<WebRequestEventArgs> SendingRequest = delegate { };

        public PackageDownloader()
            : this(new ZipPackageFactory(), new CryptoHashProvider())
        {
        }

        public PackageDownloader(IPackageFactory packageFactory, IHashProvider hashProvider)
        {
            if (packageFactory == null)
            {
                throw new ArgumentNullException("packageFactory");
            }

            if (hashProvider == null)
            {
                throw new ArgumentNullException("hashProvider");
            }

            _packageFactory = packageFactory;
            _hashProvider = hashProvider;
        }
        public IPackage DownloadPackage(Uri uri, byte[] packageHash, IPackageMetadata package)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            if (packageHash == null)
            {
                throw new ArgumentNullException("packageHash");
            }
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            var downloadClient = new HttpClient(uri);
            downloadClient.UserAgent = HttpUtility.CreateUserAgentString(DefaultUserAgentClient);
            return DownloadPackage(downloadClient, packageHash, package);
        }

        public IPackage DownloadPackage(IHttpClient downloadClient, byte[] packageHash, IPackageMetadata package)
        {
            if (downloadClient == null)
            {
                throw new ArgumentNullException("downloadClient");
            }
            if (packageHash == null)
            {
                throw new ArgumentNullException("packageHash");
            }
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            // Get the operation display text
            string operation = String.Format(CultureInfo.CurrentCulture, NuGetResources.DownloadProgressStatus, package.Id, package.Version);

            EventHandler<ProgressEventArgs> progressAvailableHandler = (sender, e) =>
            {
                OnPackageDownloadProgress(new ProgressEventArgs(operation, e.PercentComplete));
            };

            EventHandler<WebRequestEventArgs> beforeSendingRequesthandler = (sender, e) =>
            {
                OnSendingRequest(e.Request);
            };

            try
            {
                downloadClient.ProgressAvailable += progressAvailableHandler;
                downloadClient.SendingRequest += beforeSendingRequesthandler;

                // TODO: This gets held onto in memory which we want to get rid of eventually
                byte[] buffer = downloadClient.DownloadData();

                if (!_hashProvider.VerifyHash(buffer, packageHash))
                {
                    throw new InvalidDataException(NuGetResources.PackageContentsVerifyError);
                }

                return _packageFactory.CreatePackage(() =>
                {
                    return new MemoryStream(buffer);
                });
            }
            finally
            {
                downloadClient.ProgressAvailable -= progressAvailableHandler;
                downloadClient.SendingRequest -= beforeSendingRequesthandler;
            }
        }

        private void OnPackageDownloadProgress(ProgressEventArgs e)
        {
            ProgressAvailable(this, e);
        }

        private void OnSendingRequest(WebRequest webRequest)
        {
            SendingRequest(this, new WebRequestEventArgs(webRequest));
        }
    }
}