using System;
using System.Net;

namespace NuGet {
    public interface IHttpClient : IHttpClientEvents {
        string UserAgent {
            get;
            set;
        }
        Uri Uri {
            get;
            set;
        }
        IWebProxy Proxy {
            get;
            set;
        }

        IProxyFinder ProxyFinder {
            get;
            set;
        }

        bool AcceptCompression {
            get;
            set;
        }

        WebRequest CreateRequest();
        void InitializeRequest(WebRequest request);
        byte[] DownloadData();
    }
}