using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Client.V3Shim
{
    internal class ShimCallContext : InterceptCallContext, IDisposable
    {
        private WebRequest _request;
        private ManualResetEventSlim _sem;
        private MemoryStream _data;
        private string _contentType;
        private InterceptorArguments _args;
        private HttpStatusCode _statusCode;
        private MemoryStream _requestStream;
        private string _batchBoundaryId;

        public ShimCallContext(WebRequest request, MemoryStream requestStream)
            :base()
        {
            _request = request;
            _statusCode = HttpStatusCode.OK;
            _requestStream = requestStream;
            _sem = new ManualResetEventSlim(false);
        }

        public override WebRequest Request
        {
            get { return _request; }
        }

        public override MemoryStream RequestStream
        {
            get { return _requestStream; }
        }

        public override Uri RequestUri
        {
            get
            {
                return _request.RequestUri;
            }
        }

        public override string ResponseContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                _contentType = value;
            }
        }

        public override HttpStatusCode StatusCode
        {
            get
            {
                return _statusCode;
            }
            set
            {
                _statusCode = value;
            }
        }

        public Stream Data
        {
            get
            {
                _sem.Wait();
                return _data;
            }
        }

        public override InterceptorArguments Args
        {
            get
            {
                if (_args == null)
                {
                    _args = new InterceptorArguments(RequestUri);
                }

                return _args;
            }
        }

        public override Task WriteResponseAsync(byte[] data)
        {
            return Task.Run(() =>
                {
                    if (IsBatchRequest)
                    {
                        // batch requests need to be wrapped
                        ResponseContentType = String.Format(CultureInfo.InvariantCulture, "multipart/mixed;boundary=batchresponse_{0}", _batchBoundaryId);
                        string s = InterceptFormatting.MakeBatchEntry(_batchBoundaryId, data);
                        _data = new MemoryStream(Encoding.UTF8.GetBytes(s));
                    }
                    else
                    {
                        _data = new MemoryStream(data);
                    }

                    _sem.Set();
                });
        }

        public void Dispose()
        {
            if (_sem != null)
            {
                _sem.Dispose();
                _sem = null;
            }
        }

        public void UnBatch()
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(_request.Method, "POST") && RequestStream != null)
            {
                string[] postData = Encoding.UTF8.GetString(RequestStream.ToArray()).Split(new char[] { '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string url = string.Empty;

                for (int i = 0; i < postData.Length; i++)
                {
                    if (StringComparer.Ordinal.Equals(postData[i].Trim(), "GET") && i + 1 < postData.Length)
                    {
                        url = postData[i + 1].Trim();
                        break;
                    }
                }

                if (!String.IsNullOrEmpty(url))
                {
                    IsBatchRequest = true;
                    _batchBoundaryId = _request.ContentType.Split(new string[] { "batch_" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
                    _request = HttpWebRequest.CreateHttp(url);
                }
            }
        }
    }
}
