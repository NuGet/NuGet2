using Microsoft.Data.OData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace NuGet.ShimV3
{
    internal class ShimResponseMessage : IODataResponseMessage
    {
        public WebResponse WebResponse { get; private set; }
        private int _statusCode;

        public ShimResponseMessage(WebResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            _statusCode = 200;
            WebResponse = response;
        }

        public string GetHeader(string headerName)
        {
            return WebResponse.Headers.Get(headerName);
        }

        public Stream GetStream()
        {
            return WebResponse.GetResponseStream();
        }

        public IEnumerable<KeyValuePair<string, string>> Headers
        {
            get
            {
                List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();

                foreach (var header in WebResponse.Headers.AllKeys)
                {
                    headers.Add(new KeyValuePair<string, string>(header, WebResponse.Headers.Get(header)));
                }

                return headers;
            }
        }

        public void SetHeader(string headerName, string headerValue)
        {
            WebResponse.Headers.Set(headerName, headerValue);
        }

        public int StatusCode
        {
            get
            {
                return _statusCode;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
