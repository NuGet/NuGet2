using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using NuGet.Utility;

namespace NuGet.Repositories
{
	// Default implementation of the Credentials provider which will return nothing
	// by default but can be used as a base class for a more elaborate Proxy Credential Provider
    public class DefaultCredentialProvider: ICredentialProvider
    {
        public virtual ICredentials GetCredentials(ProxyType proxyType, Uri uri)
        {
            return GetCredentials(proxyType, uri, false);
        }

        public virtual ICredentials GetCredentials(ProxyType proxyType, Uri uri, bool forcePrompt)
        {
            return null;
        }
    }
}
