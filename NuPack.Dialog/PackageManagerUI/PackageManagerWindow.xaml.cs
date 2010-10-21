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

        public PackageManagerWindow(DTEPackage ownerPackage)
            : base(F1Keyword) {

            InitializeComponent();

            System.Diagnostics.Debug.Assert(ownerPackage != null);
            _ownerPackage = ownerPackage;

            SetupProviders();
        }

        private void SetupProviders() {
            VSPackageManager packageManager = new VSPackageManager(DTEExtensions.DTE);
            EnvDTE.Project activeProject = DTEExtensions.DTE.GetActiveProject();

            ProjectManager projectManager = packageManager.GetProjectManager(activeProject);

            // The ExtensionsExplorer control display providers in reverse order.
            // We want the providers to appear as Installed - Online - Updates

            var updatesProvider = new UpdatesProvider(packageManager, projectManager, Resources);
            explorer.Providers.Add(updatesProvider);

            var onlineProvider = new OnlineProvider(
                packageManager,
                projectManager,
                PackageRepositoryFactory.Default,
                VSPackageSourceProvider.GetSourceProvider(DTEExtensions.DTE),
                Resources);
            explorer.Providers.Add(onlineProvider);

            var installedProvider = new InstalledProvider(projectManager, Resources);
            explorer.Providers.Add(installedProvider);

            // make the Installed provider as selected by default
            explorer.SelectedProvider = installedProvider;
        }

        private void CanExecuteCommandOnPackage(object sender, CanExecuteRoutedEventArgs e) {

            if (OperationCoordinator.IsBusy) {
                e.CanExecute = false;
                return;
            }

            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                e.CanExecute = false;
                return;
            }

            PackageItem selectedItem = control.SelectedExtension as PackageItem;
            if (selectedItem == null) {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = selectedItem.IsEnabled;
        }

        private void ExecutedInstallPackage(object sender, ExecutedRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                return;
            }

            PackageItem selectedItem = control.SelectedExtension as PackageItem;
            if (selectedItem == null) {
                return;
            }

            OnlineProvider provider = control.SelectedProvider as OnlineProvider;
            if (provider == null) {
                return;
            }

            bool accepted = ShowLicenseWindowIfRequired(selectedItem, provider);
            if (accepted) {
                provider.Install(selectedItem);
                e.Handled = true;
            }
        }

        private void ExecutedUninstallPackage(object sender, ExecutedRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                return;
            }

            PackageItem selectedItem = control.SelectedExtension as PackageItem;
            if (selectedItem == null) {
                return;
            }

            InstalledProvider provider = control.SelectedProvider as InstalledProvider;
            if (provider == null) {
                return;
            }

            try {
                provider.Uninstall(selectedItem);

                // Remove the item from the "All" tree of installed packages
                var allTree = provider.ExtensionsTree.Nodes.First();
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

        private void ExecutedUpdatePackage(object sender, ExecutedRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                return;
            }

            PackageItem selectedItem = control.SelectedExtension as PackageItem;
            if (selectedItem == null) {
                return;
            }

            UpdatesProvider provider = control.SelectedProvider as UpdatesProvider;
            if (provider == null) {
                return;
            }

            bool accepted = ShowLicenseWindowIfRequired(selectedItem, provider);
            if (accepted) {
                provider.Update(selectedItem);
                e.Handled = true;
            }
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

            PackageItem selectedItem = control.SelectedExtension as PackageItem;
            if (selectedItem == null) {
                return;
            }

            UriHelper.OpenLicenseLink(selectedItem.LicenseUrl);
            e.Handled = true;
        }

        private void ExecuteSetFocusOnSearchBox(object sender, ExecutedRoutedEventArgs e) {
            explorer.SetFocusOnSearchBox();
        }

        // TODO: the dynamic parameter is temporary until GetPackageDependencyGraph is brought into Core
        private bool ShowLicenseWindowIfRequired(PackageItem selectedItem, dynamic provider) {
            IEnumerable<IPackage> packageGraph = provider.GetPackageDependencyGraph(selectedItem.PackageIdentity);
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
            PackagesTreeNodeBase selectedNode = explorer.SelectedExtensionTreeNode as PackagesTreeNodeBase;
            if (selectedNode != null) {
                // notify the selected node that it is opened.
                selectedNode.OnOpened();
            }
        }

        private void OnDialogWindowClosed(object sender, EventArgs e) {
            explorer.Providers.Clear();
        }
    }
}