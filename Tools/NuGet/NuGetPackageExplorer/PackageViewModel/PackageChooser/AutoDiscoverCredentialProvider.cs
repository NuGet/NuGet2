using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using NuGet.Utility;
using Kerr;
using System.Windows.Forms;

namespace NuGet.Repositories
{
    public class AutoDiscoverCredentialProvider : DefaultCredentialProvider
    {
        public override ICredentials GetCredentials(ProxyType proxyType, Uri uri, bool forcePrompt)
        {
            if (null == uri)
            {
                throw new ArgumentNullException("uri");
            }
            ICredentials credentials = null;
            switch (proxyType)
            {
                case ProxyType.None:
                    // We should not be using a proxy so don't return any credentials
                    credentials = null;
                    break;
                case ProxyType.IntegratedAuth:
                    // based on the implementation of the UseDefaultCredentials property on the HttpWebRequest object
                    // return the same credentials instead of setting the property this way the consumer of this
                    // service can determine how to use these credentials since this provider is only responsible
                    // for returning a set of credentials and not use them.
                    credentials = CredentialCache.DefaultCredentials;
                    break;
                case ProxyType.BasicAuth:
                    // Basic authentication requires that we ask the user for credentials unless they were persisted
                    credentials = GetBasicCredentials(uri, forcePrompt);
                    break;
            }
            return credentials;
        }

        private ICredentials GetBasicCredentials(Uri uri, bool forcePrompt)
        {
            string proxyHost = uri.Host;

            ICredentials basicCredentials = null;

            using (PromptForCredential dialog = new PromptForCredential())
            {
                dialog.TargetName = proxyHost;
                dialog.Title = string.Format("Connect to: {0}", proxyHost);
                dialog.GenericCredentials = true;
                dialog.AlwaysShowUI = forcePrompt;
                if (DialogResult.OK == dialog.ShowDialog())
                {
                    basicCredentials = new NetworkCredential(dialog.UserName, dialog.Password);
                }
            }

            return basicCredentials;
        }
    }
}