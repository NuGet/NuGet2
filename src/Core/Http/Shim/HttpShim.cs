using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NuGet
{
    /// <summary>
    /// HttpShim is a singleton that provides an event OnWebRequest for modifying WebRequests before they
    /// are executed.
    /// </summary>
    public sealed class HttpShim
    {
        private static HttpShim _instance;
        private Func<DataServiceClientRequestMessageArgs, DataServiceClientRequestMessage> _dataServiceHandler;
        private Func<WebRequest, WebResponse> _webHandler;

        internal HttpShim()
        {

        }

        /// <summary>
        ///  Static instance of the shim.
        /// </summary>
        public static HttpShim Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HttpShim();
                }

                return _instance;
            }
        }

        internal WebResponse ShimWebRequest(WebRequest request)
        {
            WebResponse response = null;

            if (_webHandler != null)
            {
                response = _webHandler(request);
            }
            else
            {
                response = request.GetResponse();
            }

            return response;
        }

        internal DataServiceClientRequestMessage ShimDataServiceRequest(DataServiceClientRequestMessageArgs args)
        {
            DataServiceClientRequestMessage message = null;

            if (_dataServiceHandler != null)
            {
                message = _dataServiceHandler(args);
            }
            else
            {
                message = new HttpWebRequestMessage(args);
            }

            return message;
        }

        public void SetWebRequestHandler(Func<WebRequest, WebResponse> handler)
        {
            Debug.Assert(_webHandler == null, "handler already set");

            _webHandler = handler;
        }

        public void SetDataServiceRequestHandler(Func<DataServiceClientRequestMessageArgs, DataServiceClientRequestMessage> handler)
        {
            Debug.Assert(_dataServiceHandler == null, "handler already set");

            _dataServiceHandler = handler;
        }

        public void ClearHandlers()
        {
            _dataServiceHandler = null;
            _webHandler = null;
        }
    }
}
