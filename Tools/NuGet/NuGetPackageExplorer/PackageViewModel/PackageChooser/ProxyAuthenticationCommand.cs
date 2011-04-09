using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Net;
using NuGet.Utility;

namespace PackageExplorerViewModel.PackageChooser
{
    public sealed class ProxyAuthenticationCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        public void Execute(object parameter)
        {
            CredentialsDialog dialog = new CredentialsDialog("something");
            bool shouldCancel = false;
            while (!shouldCancel)
            {
                if (MessageBoxResult.OK == dialog.Show() && AreCredentialsValid(dialog.Name,dialog.Password))
                {
                    shouldCancel = true;
                }                
            }
        }

        private bool AreCredentialsValid(string username, string password)
        {
            WebProxy currentProxy = HttpWebRequest.DefaultWebProxy;

            NetworkCredential credentials = new NetworkCredential(username,password);
            WebProxy newProxy = new WebProxy(currentProxy.Address, currentProxy.BypassProxyOnLocal, currentProxy.BypassList, credentials);
            HttpWebRequest.DefaultWebProxy = newProxy;

            HttpClientUtility.CanConnect("http://www.google.com");
        }
    }
}
