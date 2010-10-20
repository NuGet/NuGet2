using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using Microsoft.VisualStudio.PlatformUI;
using NuPack.Dialog.Providers;
using NuPack.VisualStudio;
using DTEPackage = Microsoft.VisualStudio.Shell.Package;

namespace NuPack.Dialog.PackageManagerUI {

    public partial class PackageManagerWindow : DialogWindow {
        private const string F1Keyword = "vs.ExtensionManager";
        private readonly DTEPackage _ownerPackage;
        private readonly OnlinePackagesProvider _installedPackagesProvider;
        private readonly OnlinePackagesProvider _onlinePackagesProvider;

        public PackageManagerWindow(DTEPackage ownerPackage)
            : base(F1Keyword) {

            InitializeComponent();

            System.Diagnostics.Debug.Assert(ownerPackage != null);
            _ownerPackage = ownerPackage;

            VSPackageManager packageManager = new VSPackageManager(DTEExtensions.DTE);
            EnvDTE.Project activeProject = DTEExtensions.DTE.GetActiveProject();

            UpdatePackagesProvider updatePackagesProvider = new UpdatePackagesProvider(packageManager, activeProject, Resources);
            this.explorer.Providers.Add(updatePackagesProvider);
           
            _onlinePackagesProvider = new OnlinePackagesProvider(packageManager, activeProject, Resources);
            this.explorer.Providers.Add(_onlinePackagesProvider);

            _installedPackagesProvider = new InstalledPackagesProvider(packageManager, activeProject, Resources);
            this.explorer.Providers.Add(_installedPackagesProvider);
            this.explorer.SelectedProvider = _installedPackagesProvider;
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
                provider.Install(selectedItem);
                e.Handled = true;
            }
        }

        private void CanExecuteInstallPackage(object sender, CanExecuteRoutedEventArgs e) {

            if (OperationCoordinator.IsBusy) {
                e.CanExecute = false;
                return;
            }

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

            // Don't allow the download command on packages that are already installed.
            e.CanExecute = !selectedItem.IsInstalled;
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

            try {
                provider.Uninstall(selectedItem);

                // Remove the item from the "All" tree of installed packages
                var allTree = _installedPackagesProvider.ExtensionsTree.Nodes.First();
                allTree.Extensions.Remove(selectedItem);

                e.Handled = true;
            }
            catch (InvalidOperationException ex) {
                MessageBox.Show(
                    ex.Message,
                    NuPack.Dialog.Resources.Dialog_MessageBoxTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CanExecuteUninstallPackage(object sender, CanExecuteRoutedEventArgs e) {
            if (OperationCoordinator.IsBusy) {
                e.CanExecute = false;
                return;
            }

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
                provider.Update(selectedItem);
                e.Handled = true;
            }
        }

        private void CanExecuteUpdatePackage(object sender, CanExecuteRoutedEventArgs e) {
            if (OperationCoordinator.IsBusy) {
                e.CanExecute = false;
                return;
            }

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
            _ownerPackage.ShowOptionPage(typeof(ToolsOptionsUI.ToolsOptionsPage));
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

        private void ExecuteSetFocusOnSearchBox(object sender, ExecutedRoutedEventArgs e) {
            explorer.SetFocusOnSearchBox();
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

        private void OnCategorySelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            OnlinePackagesTreeBase selectedNode = explorer.SelectedExtensionTreeNode as OnlinePackagesTreeBase;
            if (selectedNode != null) {
                // notify the selected node that it is opened.
                selectedNode.OnOpened();
            }
        }
    }
}