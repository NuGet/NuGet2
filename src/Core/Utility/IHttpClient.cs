using System;
using System.Net;

namespace NuGet {
    public interface IHttpClient : IHttpClientEvents {
        string UserAgent {
            get;
            set;
        }

        WebRequest CreateRequest(Uri uri, bool acceptCompression);
        void InitializeRequest(WebRequest request, bool acceptCompression);
        Uri GetRedirectedUri(Uri uri);

        byte[] DownloadData(Uri uri);
    }
}