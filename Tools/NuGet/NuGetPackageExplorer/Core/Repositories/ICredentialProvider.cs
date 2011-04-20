using System;
using System.Net;

namespace NuGet {
    public interface ICredentialProvider {
        bool HasCredentials(Uri uri);

        ICredentials[] GetCredentials(Uri uri);
        ICredentials PromptUserForCredentials(Uri uri, bool retryCredentials);

        ICredentials DefaultCredentials { get; }
    }
}