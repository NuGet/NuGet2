using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using Microsoft.VisualStudio.PlatformUI;
using NuGet.Dialog.Providers;
using NuGet.OutputWindowConsole;
using NuGet.VisualStudio;
using DTEPackage = Microsoft.VisualStudio.Shell.Package;

namespace NuGet.Dialog.PackageManagerUI {

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export]
    public partial class PackageManagerWindow : DialogWindow {

        private const string F1Keyword = "vs.ExtensionManager";

        private readonly DTEPackage _ownerPackage;

        [ImportingConstructor]
        public PackageManagerWindow(DTEPackage ownerPackage,
                                    DTE dte,
                                    IVsPackageManagerFactory packageManagerFactory,
                                    IPackageRepositoryFactory repositoryFactory,
                                    IPackageSourceProvider packageSourceProvider,
                                    ProviderServices providerServices,
                                    [Import(ContractConstants.RecentPackagesRepositoryContractName)]
                                    IPackageRepository recentPackagesRepository)
            : base(F1Keyword) {

            InitializeComponent();

            InsertDisclaimerElement();

            System.Diagnostics.Debug.Assert(ownerPackage != null);
            _ownerPackage = ownerPackage;

            SetupProviders(
                dte, 
                packageManagerFactory, 
                repositoryFactory, 
                packageSourceProvider,
                providerServices,
                recentPackagesRepository);
        }

        private void SetupProviders(DTE dte,
                                    IVsPackageManagerFactory packageManagerFactory,
                                    IPackageRepositoryFactory packageRepositoryFactory,
                                    IPackageSourceProvider packageSourceProvider,
                                    ProviderServices providerServices,
                                    IPackageRepository recentPackagesRepository) {

            IVsPackageManager packageManager = packageManagerFactory.CreatePackageManager();
            Project activeProject = dte.GetActiveProject();

            // Create a cached project manager so that checking for installed packages is fast
            IProjectManager projectManager = new CachedProjectManager(packageManager.GetProjectManager(activeProject));

            var recentProvider = new RecentProvider(
                activeProject,
                projectManager,
                Resources,
                packageManagerFactory,
                recentPackagesRepository,
                providerServices);

            var updatesProvider = new UpdatesProvider(
                activeProject,
                projectManager,
                Resources,
                packageRepositoryFactory,
                packageSourceProvider,
                packageManagerFactory,
                providerServices);

            var onlineProvider = new OnlineProvider(
                activeProject,
                projectManager,
                Resources,
                packageRepositoryFactory,
                packageSourceProvider,
                packageManagerFactory,
                providerServices);

            var installedProvider = new InstalledProvider(
                packageManager, 
                activeProject,
                projectManager, 
                Resources,
                providerServices);
            
            explorer.Providers.Add(recentProvider);
            explorer.Providers.Add(updatesProvider);
            explorer.Providers.Add(installedProvider);
            explorer.Providers.Add(onlineProvider);

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
                    provider.Execute(selectedItem);
                }
                catch (Exception exception) {
                    MessageHelper.ShowErrorMessage(exception);
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

        /// <summary>
        /// HACK HACK: Insert the disclaimer element into the correct place inside the Explorer control. 
        /// We don't want to bring in the whole control template of the extension explorer control.
        /// </summary>
        private void InsertDisclaimerElement() {
            Grid grid = LogicalTreeHelper.FindLogicalNode(explorer, "resGrid") as Grid;
            if (grid != null) {

                // m_Providers is the name of the expander provider control (the one on the leftmost column)
                UIElement providerExpander = FindChildElementByNameOrType(grid, "m_Providers", typeof(ProviderExpander));
                if (providerExpander != null) {
                    // remove disclaimer text and provider expander from their current parents
                    grid.Children.Remove(providerExpander);
                    LayoutRoot.Children.Remove(DisclaimerText);

                    // create the inner grid which will host disclaimer text and the provider extender
                    Grid innerGrid = new Grid();
                    innerGrid.RowDefinitions.Add(new RowDefinition());
                    innerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });

                    innerGrid.Children.Add(providerExpander);

                    Grid.SetRow(DisclaimerText, 1);
                    innerGrid.Children.Add(DisclaimerText);

                    // add the inner grid to the first column of the original grid
                    grid.Children.Add(innerGrid);
                }
            }
        }

        private UIElement FindChildElementByNameOrType(Grid parent, string childName, Type childType) {
            UIElement element = parent.FindName(childName) as UIElement;
            if (element != null) {
                return element;
            }
            else {
                foreach (UIElement child in parent.Children) {
                    if (childType.IsInstanceOfType(child)) {
                        return child;
                    }
                }
                return null;
            }
        }

        private void OnProviderSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            var selectedProvider = explorer.SelectedProvider as PackagesProviderBase;
            if (selectedProvider != null) {
                explorer.NoItemsMessage = selectedProvider.NoItemsMessage;
            }
        }
    }
}