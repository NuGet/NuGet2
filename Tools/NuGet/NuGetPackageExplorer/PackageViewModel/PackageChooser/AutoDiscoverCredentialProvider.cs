using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using NuGet.Utility;
using Kerr;

namespace NuGet.Repositories
{
    public class AutoDiscoverCredentialProvider : DefaultCredentialProvider
    {
        public override ICredentials PromptUserForCredentials(Uri uri, bool retryCredentials)
        {
            return GetBasicCredentials(uri, retryCredentials);
        }

        public override ICredentials[] GetCredentials(Uri uri)
        {
            if (null == uri)
            {
                throw new ArgumentNullException("uri");
            }
            CredentialSet set = new CredentialSet(uri.Host);
            if (null == set || set.Count < 1)
            {
                return new ICredentials[0] { };
            }
            return set
                .Select(cred => new NetworkCredential(cred.UserName, cred.Password))
                .ToArray();
        }

        private ICredentials GetBasicCredentials(Uri uri, bool forcePrompt)
        {
            string proxyHost = uri.Host;

            ICredentials basicCredentials = null;

            using (PromptForCredential dialog = new PromptForCredential())
            {
                dialog.TargetName = string.Format("PackageExplorer_{0}", proxyHost);
                dialog.Title = string.Format("Connect to {0}", proxyHost);
                dialog.Message = dialog.Title;
                dialog.GenericCredentials = true;
                dialog.AlwaysShowUI = forcePrompt;
                if (dialog.ShowDialog())
                {
                    basicCredentials = new NetworkCredential(dialog.UserName, dialog.Password);
                }
            }

            return basicCredentials;
        }
    }
}