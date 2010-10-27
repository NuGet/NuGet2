using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using Microsoft.VisualStudio.PlatformUI;
using NuGet.Dialog.Providers;
using NuGet.VisualStudio;
using DTEPackage = Microsoft.VisualStudio.Shell.Package;

namespace NuGet.Dialog.PackageManagerUI {

    public partial class PackageManagerWindow : DialogWindow, ILicenseWindowOpener {

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
            VsPackageManager packageManager = new VsPackageManager(DTEExtensions.DTE);
            EnvDTE.Project activeProject = DTEExtensions.DTE.GetActiveProject();

            IProjectManager projectManager = packageManager.GetProjectManager(activeProject);

            // The ExtensionsExplorer control display providers in reverse order.
            // We want the providers to appear as Installed - Online - Updates

            var updatesProvider = new UpdatesProvider(packageManager, projectManager, Resources);
            explorer.Providers.Add(updatesProvider);

            var onlineProvider = new OnlineProvider(packageManager, projectManager, Resources, CachedRepositoryFactory.Instance, VsPackageSourceProvider.GetSourceProvider(DTEExtensions.DTE));
            explorer.Providers.Add(onlineProvider);

            var installedProvider = new InstalledProvider(packageManager, projectManager, Resources);
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

            try {
                e.CanExecute = selectedItem.IsEnabled;
            }
            catch (Exception) {
                e.CanExecute = false;
            }
        }

        private void ExecutedPackageCommand(object sender, ExecutedRoutedEventArgs e) {
            if (OperationCoordinator.IsBusy) {
                return;
            }

            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                return;
            }

            PackageItem selectedItem = control.SelectedExtension as PackageItem;
            if (selectedItem == null) {
                return;
            }

            PackagesProviderBase provider = control.SelectedProvider as PackagesProviderBase;
            if (provider != null) {
                try {
                    provider.Execute(selectedItem, this);
                }
                catch (Exception exception) {
                    MessageBox.Show(
                        (exception.InnerException ?? exception).Message,
                        NuGet.Dialog.Resources.Dialog_MessageBoxTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void ExecutedClose(object sender, ExecutedRoutedEventArgs e) {
            this.Close();
        }

        private void ExecutedShowOptionsPage(object sender, ExecutedRoutedEventArgs e) {
            this.Close();
            _ownerPackage.ShowOptionPage(typeof(ToolsOptionsUI.ToolsOptionsPage));
        }

        private void ExecuteOpenLicenseLink(object sender, ExecutedRoutedEventArgs e) {
            Hyperlink hyperlink = e.OriginalSource as Hyperlink;
            if (hyperlink != null && hyperlink.NavigateUri != null) {
                UriHelper.OpenExternalLink(hyperlink.NavigateUri);
                e.Handled = true;
            }
        }

        private void ExecuteSetFocusOnSearchBox(object sender, ExecutedRoutedEventArgs e) {
            explorer.SetFocusOnSearchBox();
        }

        bool ILicenseWindowOpener.ShowLicenseWindow(IEnumerable<IPackage> dataContext) {
            var licenseWidow = new LicenseAcceptanceWindow() {
                Owner = this,
                DataContext = dataContext
            };

            bool? dialogResult = licenseWidow.ShowDialog();
            return dialogResult ?? false;
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
