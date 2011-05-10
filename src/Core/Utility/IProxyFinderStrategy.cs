using System;
using System.Net;

namespace NuGet {
    public interface IProxyFinderStrategy {
        IWebProxy GetProxy(Uri uri);
    }
}