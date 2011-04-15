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
        public virtual ICredentials DefaultCredentials
        {
            get { return CredentialCache.DefaultCredentials; }
        }

        public virtual bool HasCredentials(Uri uri)
        {
            ICredentials[] credentials = GetCredentials(uri);
            return credentials.Count() > 0;
        }

        public virtual ICredentials[] GetCredentials(Uri uri)
        {
            return null;
        }

        public virtual ICredentials PromptUserForCredentials(Uri uri, bool retryCredentials)
        {
            throw new NotImplementedException();
        }

    }
}
