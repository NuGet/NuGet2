using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Threading;

namespace NuGet.Dialog.PackageManagerUI {

    [Export(typeof(ILicenseWindowOpener))]
    public sealed class LicenseWindowOpener : ILicenseWindowOpener {

        private readonly Dispatcher _uiDispatcher;

        public LicenseWindowOpener() {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        bool ILicenseWindowOpener.ShowLicenseWindow(IEnumerable<IPackage> packages) {
            if (_uiDispatcher.CheckAccess()) {
                return ShowLicenseWindow(packages);
            }
            else {
                // Use Invoke() here to block the worker thread
                object result = _uiDispatcher.Invoke(new Func<object, bool>(ShowLicenseWindow), packages);
                return (bool)result;
            }
        }

        private bool ShowLicenseWindow(object dataContext) {

            var licenseWidow = new LicenseAcceptanceWindow() {
                DataContext = dataContext
            };

            // call ShowModal() instead of ShowDialog() so that the dialog
            // automatically centers within parent window
            bool? dialogResult = licenseWidow.ShowModal();
            return dialogResult ?? false;
        }
    }
}