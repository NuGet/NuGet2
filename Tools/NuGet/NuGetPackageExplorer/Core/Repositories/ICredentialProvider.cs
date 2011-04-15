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
        bool HasCredentials(Uri uri);
        
        ICredentials[] GetCredentials(Uri uri);
        ICredentials PromptUserForCredentials(Uri uri, bool retryCredentials);

        ICredentials DefaultCredentials { get; }
    }
}
