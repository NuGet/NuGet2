using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.Dialog.Providers;
using NuGet.VisualStudio;

namespace NuGet.Dialog.PackageManagerUI {
    public partial class PackageManagerWindow : DialogWindow {
        private const string DialogUserAgentClient = "NuGet Add Package Dialog";
        private Lazy<string> _dialogUserAgent = new Lazy<string>(() => HttpUtility.CreateUserAgentString(DialogUserAgentClient));

        private const string F1Keyword = "vs.ExtensionManager";

        private readonly IHttpClientEvents _httpClientEvents;
        private bool _hasOpenedOnlineProvider;

        private readonly SmartOutputConsoleProvider _smartOutputConsoleProvider;
        private readonly IVsUIShell _vsUIShell;
        private readonly ISelectedProviderSettings _selectedProviderSettings;
        private readonly IProductUpdateService _productUpdateService;

        public PackageManagerWindow() :
            this(ServiceLocator.GetInstance<DTE>(),
                 ServiceLocator.GetGlobalService<SVsUIShell, IVsUIShell>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<IPackageSourceProvider>(),
                 ServiceLocator.GetInstance<ProviderServices>(),
                 ServiceLocator.GetInstance<IRecentPackageRepository>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>(),
                 ServiceLocator.GetInstance<ISelectedProviderSettings>(),
                 ServiceLocator.GetInstance<IProductUpdateService>()) {
        }

        public PackageManagerWindow(DTE dte,
                                    IVsUIShell vsUIShell,
                                    IVsPackageManagerFactory packageManagerFactory,
                                    IPackageRepositoryFactory repositoryFactory,
                                    IPackageSourceProvider packageSourceProvider,
                                    ProviderServices providerServices,
                                    IRecentPackageRepository recentPackagesRepository,
                                    IHttpClientEvents httpClientEvents,
                                    ISelectedProviderSettings selectedProviderSettings,
                                    IProductUpdateService productUpdateService)
            : base(F1Keyword) {

            InitializeComponent();

            _httpClientEvents = httpClientEvents;
            if (_httpClientEvents != null) {
                _httpClientEvents.SendingRequest += OnSendingRequest;
            }

            AddUpdateBar(productUpdateService);

            _vsUIShell = vsUIShell;
            _selectedProviderSettings = selectedProviderSettings;
            _productUpdateService = productUpdateService;

            InsertDisclaimerElement();
            AdjustSortComboBoxWidth();

            // replace the ConsoleOutputProvider with SmartOutputConsoleProvider so that we can clear 
            // the console the first time an entry is written to it
            _smartOutputConsoleProvider = new SmartOutputConsoleProvider(providerServices.OutputConsoleProvider);
            providerServices = new ProviderServices(
                providerServices.LicenseWindow,
                providerServices.ProgressWindow,
                providerServices.ScriptExecutor,
                _smartOutputConsoleProvider);

            SetupProviders(
                dte,
                packageManagerFactory,
                repositoryFactory,
                packageSourceProvider,
                providerServices,
                recentPackagesRepository,
                httpClientEvents);
        }

        private void AddUpdateBar(IProductUpdateService productUpdateService) {
            var updateBar = new ProductUpdateBar(productUpdateService);
            updateBar.UpdateStarting += ExecutedClose;
            LayoutRoot.Children.Add(updateBar);
            updateBar.SizeChanged += OnUpdateBarSizeChanged;
        }

        private void OnUpdateBarSizeChanged(object sender, SizeChangedEventArgs e) {
            // when the update bar appears, we adjust the window position 
            // so that it doesn't push the main content area down
            if (e.HeightChanged) {
                double heightDifference = e.NewSize.Height - e.PreviousSize.Height;
                if (heightDifference > 0) {
                    Top = Math.Max(0, Top - heightDifference);
                }
            }
        }

        private void SetupProviders(DTE dte,
                                    IVsPackageManagerFactory packageManagerFactory,
                                    IPackageRepositoryFactory packageRepositoryFactory,
                                    IPackageSourceProvider packageSourceProvider,
                                    ProviderServices providerServices,
                                    IPackageRepository recentPackagesRepository,
                                    IHttpClientEvents httpClientEvents) {

            IVsPackageManager packageManager = packageManagerFactory.CreatePackageManager();
            Project activeProject = dte.GetActiveProject();

            // Create a cached project manager so that checking for installed packages is fast
            IProjectManager projectManager = packageManager.GetProjectManager(activeProject);

            var recentProvider = new RecentProvider(
                activeProject,
                projectManager,
                Resources,
                packageRepositoryFactory,
                packageManagerFactory,
                recentPackagesRepository,
                packageSourceProvider,
                providerServices,
                httpClientEvents);


            var updatesProvider = new UpdatesProvider(
                activeProject,
                projectManager,
                Resources,
                packageRepositoryFactory,
                packageSourceProvider,
                packageManagerFactory,
                providerServices,
                httpClientEvents);

            var onlineProvider = new OnlineProvider(
                activeProject,
                projectManager,
                Resources,
                packageRepositoryFactory,
                packageSourceProvider,
                packageManagerFactory,
                providerServices,
                httpClientEvents);

            var installedProvider = new InstalledProvider(
                packageManager,
                activeProject,
                projectManager,
                Resources,
                providerServices,
                httpClientEvents);

            explorer.Providers.Add(installedProvider);
            explorer.Providers.Add(onlineProvider);
            explorer.Providers.Add(updatesProvider);
            explorer.Providers.Add(recentProvider);

            // retrieve the selected provider from the settings
            int selectedProvider = Math.Min(3, _selectedProviderSettings.SelectedProvider);
            explorer.SelectedProvider = explorer.Providers[selectedProvider];
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
                    MessageHelper.ShowErrorMessage(exception, NuGet.Dialog.Resources.Dialog_MessageBoxTitle);

                    ExceptionHelper.WriteToActivityLog(exception);
                }
            }
        }

        private void ExecutedClose(object sender, EventArgs e) {
            Close();
        }

        private void ExecutedShowOptionsPage(object sender, ExecutedRoutedEventArgs e) {
            Close();
            ShowOptionsPage();
        }

        private void ShowOptionsPage() {
            // GUID of our options page, defined in ToolsOptionsPage.cs
            object targetGUID = "2819C3B6-FC75-4CD5-8C77-877903DE864C";
            Guid toolsGroupGuid = VSConstants.GUID_VSStandardCommandSet97;
            _vsUIShell.PostExecCommand(ref toolsGroupGuid, VSConstants.cmdidToolsOptions, 0, ref targetGUID);
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

            // flush output messages to the Output console at once when the dialog is closed.
            _smartOutputConsoleProvider.Flush();
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

        private void AdjustSortComboBoxWidth() {
            Grid grid = LogicalTreeHelper.FindLogicalNode(explorer, "resGrid") as Grid;
            if (grid != null) {
                var sortCombo = FindChildElementByNameOrType(grid, "cmb_SortOrder", typeof(SortCombo)) as SortCombo;
                if (sortCombo != null) {
                    // The default style fixes the Sort combo control's width to 160, which is bad for localization.
                    // We fix it by setting Min width as 160, and let the control resize to content.
                    sortCombo.ClearValue(FrameworkElement.WidthProperty);
                    sortCombo.MinWidth = 160;
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

                // save the selected provider to user settings
                _selectedProviderSettings.SelectedProvider = explorer.Providers.IndexOf(selectedProvider);
                // if this is the first time online provider is opened, call to check for update
                if (selectedProvider == explorer.Providers[1] && !_hasOpenedOnlineProvider) {
                    _hasOpenedOnlineProvider = true;
                    _productUpdateService.CheckForAvailableUpdateAsync();
                }
            }
        }

        private void OnSendingRequest(object sender, WebRequestEventArgs e) {
            HttpUtility.SetUserAgent(e.Request, _dialogUserAgent.Value);
        }
    }
}