using System.Windows;
using System.Windows.Documents;

using Microsoft.VisualStudio.PlatformUI;
using System;

namespace NuPack.Dialog.PackageManagerUI
{
    /// <summary>
    /// Interaction logic for LicenseAcceptanceWindow.xaml
    /// </summary>
    public partial class LicenseAcceptanceWindow : DialogWindow
    {
        public LicenseAcceptanceWindow()
        {
            InitializeComponent();
        }

        private void OnDeclineButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void OnAcceptButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void OnViewLicenseTermsRequestNavigate(object sender, RoutedEventArgs e)
        {
            Hyperlink hyperlink = (Hyperlink)sender;
            var licenseUrl = hyperlink.NavigateUri;

            // mitigate security risk
            if (licenseUrl.IsFile || licenseUrl.IsLoopback || licenseUrl.IsUnc)
            {
                return;
            }

            string scheme = licenseUrl.Scheme;
            if (scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                // REVIEW: Will this allow a package author to execute arbitrary program on user's machine?
                // We have limited the url to be HTTP only, but is it sufficient?
                System.Diagnostics.Process.Start(licenseUrl.AbsoluteUri);
                e.Handled = true;
            }
        }
    }
}
