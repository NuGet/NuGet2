using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.ShimV3
{
    internal class ShimCallContext : InterceptCallContext, IDisposable
    {
        private WebRequest _request;
        private ManualResetEventSlim _sem;
        private MemoryStream _data;
        private string _contentType;
        private IDebugConsoleController _logger;
        private InterceptorArguments _args;

        public ShimCallContext(WebRequest request, IDebugConsoleController logger)
            :base()
        {
            _logger = logger;
            _request = request;
            _sem = new ManualResetEventSlim(false);
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
                    _data = new MemoryStream(data);
                    _sem.Set();
                });
        }

        public override void Log(string message, ConsoleColor color)
        {
            if (!String.IsNullOrEmpty(message) && _logger != null)
            {
                _logger.Log(message, color);
            }
        }

        public void Dispose()
        {
            if (_sem != null)
            {
                _sem.Dispose();
                _sem = null;
            }
        }
    }
}
