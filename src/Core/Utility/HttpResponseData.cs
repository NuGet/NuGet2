using System;
using System.Net;

namespace NuGet {
    internal class HttpResponseData {        
        public HttpResponseData(HttpWebResponse response) {
            ResponseUri = response.ResponseUri;
            StatusCode = response.StatusCode;
        }

        public Uri ResponseUri { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }
    }
}
