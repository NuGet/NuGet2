using Microsoft.Data.OData;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.IO;
using System.Net;
using NuGet;

namespace NuGet.Client.V3Shim
{
    internal class ShimDataServiceClientRequestMessage : DataServiceClientRequestMessage, IShimWebRequest, IDisposable
    {
        public ShimDataRequestMessage ShimWebRequest { get; private set; }
        public DataServiceClientRequestMessageArgs OriginalMessageArgs { get; private set; }
        private ShimController _controller;
        private MemoryStream _requestStream;

        public ShimDataServiceClientRequestMessage(ShimController controller, DataServiceClientRequestMessageArgs args)
            : base(args.Method)
        {
            OriginalMessageArgs = args;

            ShimWebRequest = new ShimDataRequestMessage(args);

            _controller = controller;
        }

        public override void Abort()
        {
            ShimWebRequest.WebRequest.Abort();
        }

        public override IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state)
        {
            return ShimWebRequest.WebRequest.BeginGetRequestStream(callback, state);
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            return ShimWebRequest.WebRequest.BeginGetResponse(callback, state);
        }

        public override ICredentials Credentials
        {
            get
            {
                return ShimWebRequest.WebRequest.Credentials;
            }
            set
            {
                ShimWebRequest.WebRequest.Credentials = value;
            }
        }

        public override Stream EndGetRequestStream(IAsyncResult asyncResult)
        {
            return ShimWebRequest.WebRequest.EndGetRequestStream(asyncResult);
        }

        public override IODataResponseMessage EndGetResponse(IAsyncResult asyncResult)
        {
            var response = ShimWebRequest.WebRequest.EndGetResponse(asyncResult);

            return new ShimResponseMessage(response);
        }

        public override string GetHeader(string headerName)
        {
            return ShimWebRequest.GetHeader(headerName);
        }

        public override IODataResponseMessage GetResponse()
        {
            var response = _controller.ShimResponse(ShimWebRequest.WebRequest, _requestStream);

            ShimResponseMessage shimResponse = new ShimResponseMessage(response);

            // clean up the request stream
            Dispose();

            return shimResponse;
        }

        public override Stream GetStream()
        {
            // the webrequest stream is not readable, we need to create our own
            if (_requestStream == null)
            {
                _requestStream = new MemoryStream();
            }

            return _requestStream;
        }

        public override IEnumerable<KeyValuePair<string, string>> Headers
        {
            get { return ShimWebRequest.Headers; }
        }

        public override string Method
        {
            get
            {
                return ShimWebRequest.Method;
            }
            set
            {
                ShimWebRequest.Method = value;
            }
        }

        public override bool SendChunked
        {
            get
            {
                return ShimWebRequest.WebRequest.SendChunked;
            }
            set
            {
                ShimWebRequest.WebRequest.SendChunked = value;
            }
        }

        public override void SetHeader(string headerName, string headerValue)
        {
            ShimWebRequest.SetHeader(headerName, headerValue);
        }

        public override int Timeout
        {
            get
            {
                return ShimWebRequest.WebRequest.Timeout;
            }
            set
            {
                ShimWebRequest.WebRequest.Timeout = value;
            }
        }

        public override Uri Url
        {
            get
            {
                return ShimWebRequest.Url;
            }
            set
            {
                ShimWebRequest.Url = value;
            }
        }

        public void Dispose()
        {
            if (_requestStream != null)
            {
                _requestStream.Dispose();
                _requestStream = null;
            }
        }

        public HttpWebRequest Request
        {
            get
            {
                return ShimWebRequest.WebRequest;
            }
        }
    }
}
