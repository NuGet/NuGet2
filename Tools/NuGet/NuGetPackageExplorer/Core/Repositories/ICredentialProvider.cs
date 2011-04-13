using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using NuGet.Utility;

namespace NuGet.Repositories
{
    public interface ICredentialProvider
    {
        ICredentials GetCredentials(ProxyType proxyType, Uri uri);
        ICredentials GetCredentials(ProxyType proxyType, Uri uri, bool forcePrompt);
    }
}
