using System;
using System.Net;

namespace NuGet {
    public interface IProxyProvider {
        IWebProxy GetProxy(Uri uri);
    }
}