using Microsoft.Data.OData;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.IO;
using System.Net;

namespace NuGet.ShimV3
{
    internal class ShimDataServiceClientRequestMessage : DataServiceClientRequestMessage
    {
        public ShimDataRequestMessage ShimWebRequest { get; private set; }
        public DataServiceClientRequestMessageArgs OriginalMessageArgs { get; private set; }

        private Stream _data;
        private ShimController _controller;

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
            var response = _controller.ShimResponse(ShimWebRequest.WebRequest);

            ShimResponseMessage shimResponse = new ShimResponseMessage(response);

            return shimResponse;
        }

        public override Stream GetStream()
        {
            if (_data == null)
            {
                _data = ShimWebRequest.GetStream();
            }

            return _data;
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
    }
}
