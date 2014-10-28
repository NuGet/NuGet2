using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Client
{
    internal class SetUserAgentHandler : DelegatingHandler
    {
        private string _userAgent;

        public SetUserAgentHandler(string userAgent, HttpMessageHandler inner) : base(inner)
        {
            _userAgent = userAgent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("User-Agent", _userAgent);
            return base.SendAsync(request, cancellationToken);
        }
    }
}