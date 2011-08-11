using System;
using System.Net;

namespace NuGet.VisualStudio {
    public class VSRequestCredentialProvider : VisualStudioCredentialProvider {
        protected override void InitializeCredentialProxy(Uri uri, IWebProxy originalProxy) {
            WebRequest.DefaultWebProxy = new WebProxy(uri);
        }
    }
}
