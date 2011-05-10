using System;
using System.Net;

namespace NuGet {

    /// <summary>
    /// BaseProxyFinderStrategy provides a basic implementation of a IProxyFinderStrategy interface
    /// and also exposes the ability to ask for the sytems default proxy so that the same logic does
    /// not need to be repeated in every single strategy.
    /// </summary>
    public abstract class BaseProxyFinderStrategy: IProxyFinderStrategy {
        public abstract IWebProxy GetProxy(Uri uri);

        protected static WebProxy GetSystemProxy(Uri uri) {
            if(uri == null) {
                throw new ArgumentNullException("uri");
            }
            var systemProxyAddress = WebRequest.DefaultWebProxy.GetProxy(uri);
            return new WebProxy(systemProxyAddress);
        }
    }
}
