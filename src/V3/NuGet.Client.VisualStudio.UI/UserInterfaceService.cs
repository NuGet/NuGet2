using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Windows.Threading;
using System.Windows;
using NuGet.VisualStudio;

namespace NuGet.Client.VisualStudio.UI
{
    [Export(typeof(IUserInterfaceService))]
    public class UserInterfaceService : IUserInterfaceService
    {
        private readonly Dispatcher _uiDispatcher;

        public UserInterfaceService()
        {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        public bool PromptForLicenseAcceptance(IEnumerable<PackageLicenseInfo> packages, Window ownerWindow)
        {
            if (_uiDispatcher.CheckAccess())
            {
                return PromptForLicenseAcceptanceImpl(packages, ownerWindow);
            }
            else
            {
                // Use Invoke() here to block the worker thread
                object result = _uiDispatcher.Invoke(
                    new Func<IEnumerable<PackageLicenseInfo>, Window, bool>(PromptForLicenseAcceptanceImpl),
                    packages,
                    ownerWindow);
                return (bool)result;
            }
        }

        private bool PromptForLicenseAcceptanceImpl(
            IEnumerable<PackageLicenseInfo> packages,
            Window ownerWindow)
        {
            var licenseWindow = new LicenseAcceptanceWindow()
            {
                DataContext = packages,
                Owner = ownerWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            using (NuGetEventTrigger.Instance.TriggerEventBeginEnd(
                NuGetEvent.LicenseWindowBegin,
                NuGetEvent.LicenseWindowEnd))
            {
                bool? dialogResult = licenseWindow.ShowDialog();
                return dialogResult ?? false;
            }
        }

        public void LaunchExternalLink(Uri url)
        {
            NuGet.VisualStudio.UriHelper.OpenExternalLink(url);
        }

        public void LaunchNuGetOptionsDialog()
        {
            var optionsPageActivator = ServiceLocator.GetInstance<IOptionsPageActivator>();
            optionsPageActivator.ActivatePage(OptionsPage.General, null);
        }
    }
}
