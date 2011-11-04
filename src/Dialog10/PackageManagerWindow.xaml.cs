using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Dialog.Providers;
using NuGet.VisualStudio;

namespace NuGet.Dialog
{
    public partial class PackageManagerWindow : DialogWindow
    {
        internal static PackageManagerWindow CurrentInstance;
        private const string DialogUserAgentClient = "NuGet Add Package Dialog";
        private Lazy<string> _dialogUserAgent = new Lazy<string>(() => HttpUtility.CreateUserAgentString(DialogUserAgentClient));

        private const string F1Keyword = "vs.ExtensionManager";

        private readonly IHttpClientEvents _httpClientEvents;
        private bool _hasOpenedOnlineProvider;
        private ComboBox _prereleaseComboBox;

        private readonly SmartOutputConsoleProvider _smartOutputConsoleProvider;
        private readonly IVsUIShell _vsUIShell;
        private readonly IProviderSettings _providerSettings;
        private readonly IProductUpdateService _productUpdateService;
        private readonly IOptionsPageActivator _optionsPageActivator;
        private readonly Project _activeProject;

        public PackageManagerWindow(Project project) :
            this(project,
                 ServiceLocator.GetInstance<DTE>(),
                 ServiceLocator.GetGlobalService<SVsUIShell, IVsUIShell>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<IPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IRecentPackageRepository>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>(),
                 ServiceLocator.GetInstance<IProductUpdateService>(),
                 ServiceLocator.GetInstance<IPackageRestoreManager>(),
                 ServiceLocator.GetInstance<ISolutionManager>(),
                 ServiceLocator.GetInstance<IOptionsPageActivator>())
        {
        }

        private PackageManagerWindow(Project project,
                                     DTE dte,
                                     IVsUIShell vsUIShell,
                                     IVsPackageManagerFactory packageManagerFactory,
                                     IPackageRepositoryFactory repositoryFactory,
                                     IPackageSourceProvider packageSourceProvider,
                                     IRecentPackageRepository recentPackagesRepository,
                                     IHttpClientEvents httpClientEvents,
                                     IProductUpdateService productUpdateService,
                                     IPackageRestoreManager packageRestoreManager,
                                     ISolutionManager solutionManager,
                                     IOptionsPageActivator optionPageActivator)
            : base(F1Keyword)
        {

            InitializeComponent();

            _httpClientEvents = httpClientEvents;
            if (_httpClientEvents != null)
            {
                _httpClientEvents.SendingRequest += OnSendingRequest;
            }

            _vsUIShell = vsUIShell;
            _productUpdateService = productUpdateService;
            _optionsPageActivator = optionPageActivator;
            _activeProject = project;

            // replace the ConsoleOutputProvider with SmartOutputConsoleProvider so that we can clear 
            // the console the first time an entry is written to it
            var providerServices = new ProviderServices();
            _smartOutputConsoleProvider = new SmartOutputConsoleProvider(providerServices.OutputConsoleProvider);
            providerServices.OutputConsoleProvider = _smartOutputConsoleProvider;
            _providerSettings = providerServices.ProviderSettings;

            AddUpdateBar(productUpdateService);
            AddRestoreBar(packageRestoreManager);
            InsertDisclaimerElement();
            AdjustSortComboBoxWidth();
            PreparePrereleaseComboBox();

            SetupProviders(
                project,
                dte,
                packageManagerFactory,
                repositoryFactory,
                packageSourceProvider,
                providerServices,
                recentPackagesRepository,
                httpClientEvents,
                solutionManager);
        }

        private void AddUpdateBar(IProductUpdateService productUpdateService)
        {
            var updateBar = new ProductUpdateBar(productUpdateService);
            updateBar.UpdateStarting += ExecutedClose;
            LayoutRoot.Children.Add(updateBar);
            updateBar.SizeChanged += OnHeaderBarSizeChanged;
        }

        private void AddRestoreBar(IPackageRestoreManager packageRestoreManager)
        {
            var restoreBar = new PackageRestoreBar(packageRestoreManager);
            LayoutRoot.Children.Add(restoreBar);
            restoreBar.SizeChanged += OnHeaderBarSizeChanged;
        }

        private void OnHeaderBarSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // when the update bar appears, we adjust the window position 
            // so that it doesn't push the main content area down
            if (e.HeightChanged)
            {
                double heightDifference = e.NewSize.Height - e.PreviousSize.Height;
                if (heightDifference > 0)
                {
                    Top = Math.Max(0, Top - heightDifference);
                }
            }
        }

        private void SetupProviders(Project activeProject,
                                    DTE dte,
                                    IVsPackageManagerFactory packageManagerFactory,
                                    IPackageRepositoryFactory packageRepositoryFactory,
                                    IPackageSourceProvider packageSourceProvider,
                                    ProviderServices providerServices,
                                    IPackageRepository recentPackagesRepository,
                                    IHttpClientEvents httpClientEvents,
                                    ISolutionManager solutionManager)
        {

            // This package manager is not used for installing from a remote source, and therefore does not need a fallback repository for resolving dependencies
            IVsPackageManager packageManager = packageManagerFactory.CreatePackageManager(ServiceLocator.GetInstance<IPackageRepository>(), useFallbackForDependencies: false);

            IPackageRepository localRepository;

            // we need different sets of providers depending on whether the dialog is open for solution or a project
            OnlineProvider onlineProvider;
            InstalledProvider installedProvider;
            UpdatesProvider updatesProvider;
            OnlineProvider recentProvider;

            if (activeProject == null)
            {
                Title = String.Format(
                    CultureInfo.CurrentUICulture,
                    NuGet.Dialog.Resources.Dialog_Title,
                    dte.Solution.GetName() + ".sln");

                localRepository = packageManager.LocalRepository;

                onlineProvider = new SolutionOnlineProvider(
                    localRepository,
                    Resources,
                    packageRepositoryFactory,
                    packageSourceProvider,
                    packageManagerFactory,
                    providerServices,
                    httpClientEvents,
                    solutionManager);
                installedProvider = new SolutionInstalledProvider(
                    packageManager,
                    localRepository,
                    Resources,
                    providerServices,
                    httpClientEvents,
                    solutionManager);

                updatesProvider = new SolutionUpdatesProvider(
                    localRepository,
                    Resources,
                    packageRepositoryFactory,
                    packageSourceProvider,
                    packageManagerFactory,
                    providerServices,
                    httpClientEvents,
                    solutionManager);

                recentProvider = new SolutionRecentProvider(
                    localRepository,
                    Resources,
                    packageRepositoryFactory,
                    packageManagerFactory,
                    recentPackagesRepository,
                    packageSourceProvider,
                    providerServices,
                    httpClientEvents,
                    solutionManager);
            }
            else
            {
                IProjectManager projectManager = packageManager.GetProjectManager(activeProject);
                localRepository = projectManager.LocalRepository;

                Title = String.Format(
                    CultureInfo.CurrentUICulture,
                    NuGet.Dialog.Resources.Dialog_Title,
                    activeProject.GetDisplayName());

                onlineProvider = new OnlineProvider(
                    activeProject,
                    localRepository,
                    Resources,
                    packageRepositoryFactory,
                    packageSourceProvider,
                    packageManagerFactory,
                    providerServices,
                    httpClientEvents,
                    solutionManager);

                installedProvider = new InstalledProvider(
                    packageManager,
                    activeProject,
                    localRepository,
                    Resources,
                    providerServices,
                    httpClientEvents,
                    solutionManager);

                updatesProvider = new UpdatesProvider(
                    activeProject,
                    localRepository,
                    Resources,
                    packageRepositoryFactory,
                    packageSourceProvider,
                    packageManagerFactory,
                    providerServices,
                    httpClientEvents,
                    solutionManager);

                recentProvider = new RecentProvider(
                    activeProject,
                    localRepository,
                    Resources,
                    packageRepositoryFactory,
                    packageManagerFactory,
                    recentPackagesRepository,
                    packageSourceProvider,
                    providerServices,
                    httpClientEvents,
                    solutionManager);
            }

            explorer.Providers.Add(installedProvider);
            explorer.Providers.Add(onlineProvider);
            explorer.Providers.Add(updatesProvider);
            explorer.Providers.Add(recentProvider);

            installedProvider.IncludePrerelease =
                onlineProvider.IncludePrerelease =
                updatesProvider.IncludePrerelease =
                recentProvider.IncludePrerelease = _providerSettings.IncludePrereleasePackages;

            // retrieve the selected provider from the settings
            int selectedProvider = Math.Min(3, _providerSettings.SelectedProvider);
            explorer.SelectedProvider = explorer.Providers[selectedProvider];
        }

        private void CanExecuteCommandOnPackage(object sender, CanExecuteRoutedEventArgs e)
        {
            if (OperationCoordinator.IsBusy)
            {
                e.CanExecute = false;
                return;
            }

            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null)
            {
                e.CanExecute = false;
                return;
            }

            PackageItem selectedItem = control.SelectedExtension as PackageItem;
            if (selectedItem == null)
            {
                e.CanExecute = false;
                return;
            }

            try
            {
                e.CanExecute = selectedItem.IsEnabled;
            }
            catch (Exception)
            {
                e.CanExecute = false;
            }
        }

        private void ExecutedPackageCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (OperationCoordinator.IsBusy)
            {
                return;
            }

            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null)
            {
                return;
            }

            PackageItem selectedItem = control.SelectedExtension as PackageItem;
            if (selectedItem == null)
            {
                return;
            }

            PackagesProviderBase provider = control.SelectedProvider as PackagesProviderBase;
            if (provider != null)
            {
                try
                {
                    provider.Execute(selectedItem);
                }
                catch (Exception exception)
                {
                    MessageHelper.ShowErrorMessage(exception, NuGet.Dialog.Resources.Dialog_MessageBoxTitle);

                    ExceptionHelper.WriteToActivityLog(exception);
                }
            }
        }

        private void ExecutedClose(object sender, EventArgs e)
        {
            Close();
        }

        private void ExecutedShowOptionsPage(object sender, ExecutedRoutedEventArgs e)
        {
            Close();

            _optionsPageActivator.ActivatePage(
                OptionsPage.PackageSources,
                () => OnActivated(_activeProject));
        }

        /// <summary>
        /// Called when coming back from the Options dialog
        /// </summary>
        private static void OnActivated(Project project)
        {
            var window = new PackageManagerWindow(project);
            try
            {
                window.ShowModal();
            }
            catch (TargetInvocationException exception)
            {
                MessageHelper.ShowErrorMessage(exception, NuGet.Dialog.Resources.Dialog_MessageBoxTitle);
                ExceptionHelper.WriteToActivityLog(exception);
            }
        }

        private void ExecuteOpenLicenseLink(object sender, ExecutedRoutedEventArgs e)
        {
            Hyperlink hyperlink = e.OriginalSource as Hyperlink;
            if (hyperlink != null && hyperlink.NavigateUri != null)
            {
                UriHelper.OpenExternalLink(hyperlink.NavigateUri);
                e.Handled = true;
            }
        }

        private void ExecuteSetFocusOnSearchBox(object sender, ExecutedRoutedEventArgs e)
        {
            explorer.SetFocusOnSearchBox();
        }

        private void OnCategorySelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            PackagesTreeNodeBase selectedNode = explorer.SelectedExtensionTreeNode as PackagesTreeNodeBase;
            if (selectedNode != null)
            {
                // notify the selected node that it is opened.
                selectedNode.OnOpened();
            }
        }

        private void OnDialogWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // don't allow the dialog to be closed if an operation is pending
            if (OperationCoordinator.IsBusy)
            {
                e.Cancel = true;
            }
        }

        private void OnDialogWindowClosed(object sender, EventArgs e)
        {
            explorer.Providers.Clear();

            // flush output messages to the Output console at once when the dialog is closed.
            _smartOutputConsoleProvider.Flush();

            CurrentInstance = null;
        }

        /// <summary>
        /// HACK HACK: Insert the disclaimer element into the correct place inside the Explorer control. 
        /// We don't want to bring in the whole control template of the extension explorer control.
        /// </summary>
        private void InsertDisclaimerElement()
        {
            Grid grid = LogicalTreeHelper.FindLogicalNode(explorer, "resGrid") as Grid;
            if (grid != null)
            {

                // m_Providers is the name of the expander provider control (the one on the leftmost column)
                UIElement providerExpander = FindChildElementByNameOrType(grid, "m_Providers", typeof(ProviderExpander));
                if (providerExpander != null)
                {
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

        private void AdjustSortComboBoxWidth()
        {
            ComboBox sortCombo = FindComboBox("cmd_SortOrder");
            if (sortCombo != null)
            {
                // The default style fixes the Sort combo control's width to 160, which is bad for localization.
                // We fix it by setting Min width as 160, and let the control resize to content.
                sortCombo.ClearValue(FrameworkElement.WidthProperty);
                sortCombo.MinWidth = 160;
            }
        }

        private void PreparePrereleaseComboBox()
        {
            // This ComboBox is actually used to display framework versions in various VS dialogs. 
            // We "repurpose" it here to show Prerelease option instead.
            ComboBox fxCombo = FindComboBox("cmb_Fx");
            if (fxCombo != null)
            {
                fxCombo.Items.Clear();
                fxCombo.Items.Add(NuGet.Dialog.Resources.Filter_StablePackages);
                fxCombo.Items.Add(NuGet.Dialog.Resources.Filter_IncludePrerelease);
                fxCombo.SelectedIndex = _providerSettings.IncludePrereleasePackages ? 1 : 0;
                fxCombo.SelectionChanged += OnFxComboBoxSelectionChanged;

                _prereleaseComboBox = fxCombo;
            }
        }

        private void OnFxComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = (ComboBox)sender;
            if (combo.SelectedIndex == -1) {
                return;
            }

            bool includePrerelease = combo.SelectedIndex == 1;

            // persist the option to VS settings store
            _providerSettings.IncludePrereleasePackages = includePrerelease;
            
            // set the flags on all providers
            foreach (PackagesProviderBase provider in explorer.Providers)
            {
                provider.IncludePrerelease = includePrerelease;
            }

            var selectedTreeNode = explorer.SelectedExtensionTreeNode as PackagesTreeNodeBase;
            if (selectedTreeNode != null)
            {
                selectedTreeNode.Refresh(resetQueryBeforeRefresh: true);
            }
        }

        private ComboBox FindComboBox(string name)
        {
            Grid grid = LogicalTreeHelper.FindLogicalNode(explorer, "resGrid") as Grid;
            if (grid != null)
            {
                return FindChildElementByNameOrType(grid, name, typeof(SortCombo)) as ComboBox;
            }

            return null;
        }

        private UIElement FindChildElementByNameOrType(Grid parent, string childName, Type childType)
        {
            UIElement element = parent.FindName(childName) as UIElement;
            if (element != null)
            {
                return element;
            }
            else
            {
                foreach (UIElement child in parent.Children)
                {
                    if (childType.IsInstanceOfType(child))
                    {
                        return child;
                    }
                }
                return null;
            }
        }

        private void OnProviderSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedProvider = explorer.SelectedProvider as PackagesProviderBase;
            if (selectedProvider != null)
            {
                explorer.NoItemsMessage = selectedProvider.NoItemsMessage;
                _prereleaseComboBox.Visibility = selectedProvider.ShowPrereleaseComboBox ? Visibility.Visible : Visibility.Collapsed;

                // save the selected provider to user settings
                _providerSettings.SelectedProvider = explorer.Providers.IndexOf(selectedProvider);
                // if this is the first time online provider is opened, call to check for update
                if (selectedProvider == explorer.Providers[1] && !_hasOpenedOnlineProvider)
                {
                    _hasOpenedOnlineProvider = true;
                    _productUpdateService.CheckForAvailableUpdateAsync();
                }
            }
            else
            {
                _prereleaseComboBox.Visibility = Visibility.Collapsed;
            }
        }

        private void OnSendingRequest(object sender, WebRequestEventArgs e)
        {
            HttpUtility.SetUserAgent(e.Request, _dialogUserAgent.Value);
        }

        private void CanExecuteClose(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !OperationCoordinator.IsBusy;
            e.Handled = true;
        }

        private void OnDialogWindowLoaded(object sender, RoutedEventArgs e)
        {
            // HACK: Keep track of the currently open instance of this class.
            CurrentInstance = this;
        }
    }
}