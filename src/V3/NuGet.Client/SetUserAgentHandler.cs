using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Client
{
    internal class SetUserAgentHandler : DelegatingHandler
    {
        private string _userAgent;
        private ProductInfoHeaderValue _header;

        public SetUserAgentHandler(string userAgent, HttpMessageHandler inner) : base(inner)
        {
            _userAgent = userAgent;
            _header = ProductInfoHeaderValue.Parse(_userAgent);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.Add(_header);
            return base.SendAsync(request, cancellationToken);
        }
    }
}