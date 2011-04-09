using System;
using System.Net;

namespace NuGet {
    public interface IHttpClient {
        string UserAgent {
            get;
            set;
        }
        Uri Uri { get; set; }

        //WebRequest CreateRequest(Uri uri);
        WebRequest CreateRequest();
        void InitializeRequest(WebRequest request);
        //Uri GetRedirectedUri(Uri uri);
        IHttpClient GetRedirectedClient(Uri uri);
    }
}
