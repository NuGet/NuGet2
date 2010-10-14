using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using Microsoft.VisualStudio.PlatformUI;
using NuPack.Dialog.Providers;
using DTEPackage = Microsoft.VisualStudio.Shell.Package;
using NuPack.VisualStudio;

namespace NuPack.Dialog.PackageManagerUI {
    /// <summary>
    /// Interaction logic for PackageManagerWindow.xaml
    /// </summary>
    public partial class PackageManagerWindow : DialogWindow, INotifyPropertyChanged {
        private const string F1Keyword = "vs.ExtensionManager";
        private readonly DTEPackage _ownerPackage;
        private readonly OnlinePackagesProvider _installedPackagesProvider;
        private readonly OnlinePackagesProvider _onlinePackagesProvider;

        public PackageManagerWindow(DTEPackage ownerPackage)
            : base(F1Keyword) {

            InitializeComponent();

            System.Diagnostics.Debug.Assert(ownerPackage != null);
            _ownerPackage = ownerPackage;

            VSPackageManager packageManager = VSPackageManager.GetPackageManager(DTEExtensions.DTE);
            EnvDTE.Project activeProject = DTEExtensions.DTE.GetActiveProject();

            _installedPackagesProvider = new InstalledPackagesProvider(packageManager, activeProject, Resources);
            this.explorer.Providers.Add(_installedPackagesProvider);

            UpdatePackagesProvider updatePackagesProvider = new UpdatePackagesProvider(packageManager, activeProject, Resources);
            this.explorer.Providers.Add(updatePackagesProvider);

            _onlinePackagesProvider = new OnlinePackagesProvider(packageManager, activeProject, Resources, false);
            this.explorer.Providers.Add(_onlinePackagesProvider);
            this.explorer.SelectedProvider = _onlinePackagesProvider;
        }

        private void ExecutedUninstallPackage(object sender, ExecutedRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;

            if (control == null) {
                return;
            }

            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;

            if (selectedItem == null) {
                return;
            }

            OnlinePackagesProvider provider = control.SelectedProvider as OnlinePackagesProvider;
            if (provider == null) {
                return;
            }

            provider.Uninstall(selectedItem.Id);

            // Remove the item from the "All" tree of installed packages
            var allTree = _installedPackagesProvider.ExtensionsTree.Nodes.First();
            var uninstalledItem = allTree.Extensions.FirstOrDefault(c => c.Id.Equals(selectedItem.Id, StringComparison.OrdinalIgnoreCase));

            if (uninstalledItem != null) {
                allTree.Extensions.Remove(uninstalledItem);
            }
        }

        private void CanExecuteUninstallPackage(object sender, CanExecuteRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                e.CanExecute = false;
                return;
            }
            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null) {
                e.CanExecute = false;
                return;
            }

            // Only allow the download command on packages that are already installed.
            e.CanExecute = selectedItem.IsInstalled;
        }

        private void ExecutedUpdatePackage(object sender, ExecutedRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                return;
            }

            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null) {
                return;
            }

            OnlinePackagesProvider provider = control.SelectedProvider as OnlinePackagesProvider;
            if (provider == null) {
                return;
            }

            bool accepted = ShowLicenseWindowIfRequired(selectedItem);
            if (accepted) {
                provider.Update(selectedItem.Id, new Version(selectedItem.Version));
            }
        }

        private void CanExecuteUpdatePackage(object sender, CanExecuteRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                e.CanExecute = false;
                return;
            }
            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null) {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = !selectedItem.IsUpdated;
        }


        private void ExecutedClose(object sender, ExecutedRoutedEventArgs e) {
            this.Close();
        }

        private void CanExecuteClose(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        private void ExecutedShowOptionsPage(object sender, ExecutedRoutedEventArgs e) {
            this.Close();
            OpenOptionsPage();
        }

        private void OpenOptionsPage() {
            _ownerPackage.ShowOptionPage(typeof(ToolsOptionsUI.ToolsOptionsPage));
        }

        private void ExecutedInstallPackage(object sender, ExecutedRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                return;
            }

            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null) {
                return;
            }

            OnlinePackagesProvider provider = control.SelectedProvider as OnlinePackagesProvider;
            if (provider == null) {
                return;
            }

            bool accepted = ShowLicenseWindowIfRequired(selectedItem);
            if (accepted) {
                provider.Install(selectedItem.Id, new Version(selectedItem.Version));
            }
        }

        private void CanExecuteInstallPackage(object sender, CanExecuteRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                e.CanExecute = false;
                return;
            }
            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null) {
                e.CanExecute = false;
                return;
            }

            if (selectedItem.IsInstalled) {
                //Don't allow the download command on packages that are already installed.
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        private void ExecuteOpenLicenseLink(object sender, ExecutedRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                return;
            }

            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null) {
                return;
            }

            UriHelper.OpenLicenseLink(selectedItem.LicenseUrl);
            e.Handled = true;
        }

        private void ExecutedFocusOnSearchBox(object sender, ExecutedRoutedEventArgs e) {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnNotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void DialogWindow_Closed(object sender, EventArgs e) {
            // TODO: investigate to see if there is a better fix in CTP2
            this.explorer.Providers.Clear();
        }

        private bool ShowLicenseWindowIfRequired(OnlinePackagesItem selectedItem) {
            IEnumerable<IPackage> packageGraph = _onlinePackagesProvider.GetPackageDependencyGraph(selectedItem.ExtensionRecord);
            IEnumerable<IPackage> packagesRequireLicense = packageGraph.Where(p => p.RequireLicenseAcceptance);

            if (packagesRequireLicense.Any()) {
                var licenseWidow = new LicenseAcceptanceWindow() {
                    Owner = this,
                    DataContext = packagesRequireLicense
                };

                bool? dialogResult = licenseWidow.ShowDialog();
                return dialogResult ?? false;
            }
            else {
                return true;
            }
        }
    }
}