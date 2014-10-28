using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Client
{
    internal class TracingHttpHandler : DelegatingHandler
    {
        private TraceSource _trace;

        public TracingHttpHandler(TraceSource trace, HttpMessageHandler inner) : base(inner)
        {
            _trace = trace;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _trace.Verbose("http_request", "{0} {1}", request.Method.Method, request.RequestUri.ToString());
            var resp = await base.SendAsync(request, cancellationToken);
            _trace.Verbose("http_response", "{0} {1}", (int)resp.StatusCode, request.RequestUri.ToString());
            return resp;
        }
    }
}