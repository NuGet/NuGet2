using System;
using NuGet.Utility;
using System.Net;

namespace NuGet.Repositories
{
    public interface IProxyService
    {
        IWebProxy GetProxy(Uri uri);
    }
}
