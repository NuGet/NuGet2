using System;
using System.Net;

namespace NuGet {
    /// <summary>
    /// BaseProxyProvider provides a basic implementation of a IProxyProvider interface
    /// and also exposes the ability to ask for the sytems default proxy so that the same logic does
    /// not need to be repeated in every single provider.
    /// </summary>
    public abstract class BaseProxyProvider : IProxyProvider {
        public abstract IWebProxy GetProxy(Uri uri);

        protected static WebProxy GetSystemProxy(Uri uri) {
            if (uri == null) {
                throw new ArgumentNullException("uri");
            }
            var systemProxyAddress = WebRequest.DefaultWebProxy.GetProxy(uri);
            return new WebProxy(systemProxyAddress);
        }
    }
}
