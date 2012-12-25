using System;
using System.Globalization;
using System.IO;
using System.Net;
using NuGet.Resources;

namespace NuGet
{
    public class PackageDownloader : IHttpClientEvents
    {
        private const string DefaultUserAgentClient = "NuGet Core";

        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };
        public event EventHandler<WebRequestEventArgs> SendingRequest = delegate { };

        public virtual void DownloadPackage(Uri uri, IPackageMetadata package, Stream targetStream)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            var downloadClient = new HttpClient(uri)
                                 {
                                     UserAgent = HttpUtility.CreateUserAgentString(DefaultUserAgentClient)
                                 };
            DownloadPackage(downloadClient, package, targetStream);
        }

        public void DownloadPackage(IHttpClient downloadClient, IPackageMetadata package, Stream targetStream)
        {
            if (downloadClient == null)
            {
                throw new ArgumentNullException("downloadClient");
            }

            if (targetStream == null)
            {
                throw new ArgumentNullException("targetStream");
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

                downloadClient.DownloadData(targetStream);
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