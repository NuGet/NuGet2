using System;
using System.Net;

namespace NuGet {
    /// <summary>
    /// Provides a system proxy defaulted with sytems integrated credentials for NTLM/Integrated authentication
    /// type of proxy.
    /// </summary>
    public class IntegratedCredentialsStrategy : BaseProxyFinderStrategy {
        public override IWebProxy GetProxy(Uri uri) {
            WebProxy proxy = GetSystemProxy(uri);
            proxy.Credentials = CredentialCache.DefaultCredentials;
            return proxy;
        }
    }
}