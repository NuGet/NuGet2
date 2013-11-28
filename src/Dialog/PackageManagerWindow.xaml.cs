using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public partial class PackageManagerWindow : VsDialogWindow
    {
        internal static PackageManagerWindow CurrentInstance;
        private const string DialogUserAgentClient = "NuGet VS Packages Dialog";
        private const string DialogForSolutionUserAgentClient = "NuGet VS Packages Dialog - Solution";
        private readonly Lazy<string> _dialogUserAgent = new Lazy<string>(
            () => HttpUtility.CreateUserAgentString(DialogUserAgentClient, VsVersionHelper.FullVsEdition));
        private readonly Lazy<string> _dialogForSolutionUserAgent = new Lazy<string>(
            () => HttpUtility.CreateUserAgentString(DialogForSolutionUserAgentClient, VsVersionHelper.FullVsEdition));

        private static readonly string[] Providers = new string[] { "Installed", "Online", "Updates" };
        private const string SearchInSwitch = "/searchin:";

        private const string F1Keyword = "vs.ExtensionManager";

        private readonly IHttpClientEvents _httpClientEvents;
        private bool _hasOpenedOnlineProvider;
        private ComboBox _prereleaseComboBox;

        private readonly SmartOutputConsoleProvider _smartOutputConsoleProvider;
        private readonly IProviderSettings _providerSettings;
        private readonly IProductUpdateService _productUpdateService;
        private readonly IOptionsPageActivator _optionsPageActivator;
        private readonly IUpdateAllUIService _updateAllUIService;
        private readonly Project _activeProject;
        private readonly string _projectGuids;
        private string _searchText;
        private ProductUpdateBar _updateBar = null;
        private PackageRestoreBar _restoreBar;

        public PackageManagerWindow(Project project, string dialogParameters = null) :
            this(project,
                 ServiceLocator.GetInstance<DTE>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<IPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>(),
                 ServiceLocator.GetInstance<IProductUpdateService>(),
                 ServiceLocator.GetInstance<IPackageRestoreManager>(),
                 ServiceLocator.GetInstance<ISolutionManager>(),
                 ServiceLocator.GetInstance<IOptionsPageActivator>(),
                 ServiceLocator.GetInstance<IDeleteOnRestartManager>(),
                 ServiceLocator.GetGlobalService<SVsShell, IVsShell4>(),
                 dialogParameters)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private PackageManagerWindow(Project project,
                                    DTE dte,
                                    IVsPackageManagerFactory packageManagerFactory,
                                    IPackageRepositoryFactory repositoryFactory,
                                    IPackageSourceProvider packageSourceProvider,
                                    IHttpClientEvents httpClientEvents,
                                    IProductUpdateService productUpdateService,
                                    IPackageRestoreManager packageRestoreManager,
                                    ISolutionManager solutionManager,
                                    IOptionsPageActivator optionPageActivator,
                                    IDeleteOnRestartManager deleteOnRestartManager,
                                    IVsShell4 vsRestarter,
                                    string dialogParameters)
            : base(F1Keyword)
        {

            InitializeComponent();

#if !VS10
            // set unique search guid for VS11
            explorer.SearchCategory = new Guid("{85566D5F-E585-411F-B299-5BF006E9F11E}");
#endif

            _httpClientEvents = httpClientEvents;
            if (_httpClientEvents != null)
            {
                _httpClientEvents.SendingRequest += OnSendingRequest;
            }

            _productUpdateService = productUpdateService;
            _optionsPageActivator = optionPageActivator;
            _activeProject = project;

            // replace the ConsoleOutputProvider with SmartOutputConsoleProvider so that we can clear 
            // the console the first time an entry is written to it
            var providerServices = new ProviderServices();
            _smartOutputConsoleProvider = new SmartOutputConsoleProvider(providerServices.OutputConsoleProvider);
            _smartOutputConsoleProvider.CreateOutputConsole(requirePowerShellHost: false);
            _smartOutputConsoleProvider.Clear();
            providerServices.OutputConsoleProvider = _smartOutputConsoleProvider;
            _providerSettings = providerServices.ProviderSettings;
            _updateAllUIService = providerServices.UpdateAllUIService;
            providerServices.ProgressWindow.UpgradeNuGetRequested += (_, __) =>
                {
                    Close();
                    productUpdateService.Update();
                };

            _projectGuids = _activeProject == null ? null : _activeProject.GetAllProjectTypeGuid(); 

            AddUpdateBar(productUpdateService);
            AddRestoreBar(packageRestoreManager);
            var restartRequestBar = AddRestartRequestBar(deleteOnRestartManager, vsRestarter);
            InsertDisclaimerElement();
            AdjustSortComboBoxWidth();
            PreparePrereleaseComboBox();
            InsertUpdateAllButton(providerServices.UpdateAllUIService);

            SetupProviders(
                project,
                dte,
                packageManagerFactory,
                repositoryFactory,
                packageSourceProvider,
                providerServices,
                httpClientEvents,
                solutionManager,
                packageRestoreManager,
                restartRequestBar);

            ProcessDialogParameters(dialogParameters);
        }

        /// <summary>
        /// Project.ManageNuGetPackages supports 1 optional argument and 1 optional switch /searchin. /searchin Switch has to be provided at the end
        /// If the provider specified in the optional switch is not valid, then the provider entered is ignored
        /// </summary>
        /// <param name="dialogParameters"></param>
        private void ProcessDialogParameters(string dialogParameters)
        {
            bool providerSet = false;
            if (dialogParameters != null)
            {
                dialogParameters = dialogParameters.Trim();
                int lastIndexOfSearchInSwitch = dialogParameters.LastIndexOf(SearchInSwitch, StringComparison.OrdinalIgnoreCase);

                if (lastIndexOfSearchInSwitch == -1)
                {
                    _searchText = dialogParameters;
                }
                else
                {
                    _searchText = dialogParameters.Substring(0, lastIndexOfSearchInSwitch);

                    // At this point, we know that /searchin: exists in the string.
                    // Check if there is content following the switch
                    if (dialogParameters.Length > (lastIndexOfSearchInSwitch + SearchInSwitch.Length))
                    {
                        // At this point, we know that there is some content following the /searchin: switch
                        // Check if it represents a valid provider. Otherwise, don't set the provider here
                        // Note that at the end of the method the provider from the settings will be used if no valid provider was determined
                        string provider = dialogParameters.Substring(lastIndexOfSearchInSwitch + SearchInSwitch.Length);
                        for (int i = 0; i < Providers.Length; i++)
                        {
                            // Case insensitive comparisons with the strings
                            if (String.Equals(Providers[i], provider, StringComparison.OrdinalIgnoreCase))
                            {
                                UpdateSelectedProvider(i);
                                providerSet = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!providerSet)
            {
                // retrieve the selected provider from the settings
                UpdateSelectedProvider(_providerSettings.SelectedProvider);
            }

            if (!String.IsNullOrEmpty(_searchText))
            {
                var selectedProvider = explorer.SelectedProvider as PackagesProviderBase;
                selectedProvider.SuppressLoad = true;
            }
        }

        private void AddUpdateBar(IProductUpdateService productUpdateService)
        {
            _updateBar = new ProductUpdateBar(productUpdateService);
            _updateBar.UpdateStarting += ExecutedClose;
            LayoutRoot.Children.Add(_updateBar);
            _updateBar.SizeChanged += OnHeaderBarSizeChanged;
        }

        private void RemoveUpdateBar()
        {
            if (_updateBar != null)
            {
                LayoutRoot.Children.Remove(_updateBar);
                _updateBar.CleanUp();
                _updateBar.UpdateStarting -= ExecutedClose;
                _updateBar.SizeChanged -= OnHeaderBarSizeChanged;
                _updateBar = null;
            }
        }

        private void AddRestoreBar(IPackageRestoreManager packageRestoreManager)
        {
            _restoreBar = new PackageRestoreBar(packageRestoreManager);
            LayoutRoot.Children.Add(_restoreBar);
            _restoreBar.SizeChanged += OnHeaderBarSizeChanged;
        }

        private void RemoveRestoreBar()
        {
            if (_restoreBar != null)
            {
                LayoutRoot.Children.Remove(_restoreBar);
                _restoreBar.CleanUp();
                _restoreBar.SizeChanged -= OnHeaderBarSizeChanged;
                _restoreBar = null;
            }
        }

        private RestartRequestBar AddRestartRequestBar(IDeleteOnRestartManager deleteOnRestartManager, IVsShell4 vsRestarter)
        {
            var restartRequestBar = new RestartRequestBar(deleteOnRestartManager, vsRestarter);
            Grid.SetColumn(restartRequestBar, 1);
            BottomBar.Children.Insert(1, restartRequestBar);
            return restartRequestBar;
        }

        private void OnHeaderBarSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // when the update bar appears, we adjust the window position 
            // so that it doesn't push the main content area down
            if (e.HeightChanged && e.PreviousSize.Height < 0.5)
            {
                Top = Math.Max(0, Top - e.NewSize.Height);
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope"), SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private void SetupProviders(Project activeProject,
                                    DTE dte,
                                    IVsPackageManagerFactory packageManagerFactory,
                                    IPackageRepositoryFactory packageRepositoryFactory,
                                    IPackageSourceProvider packageSourceProvider,
                                    ProviderServices providerServices,
                                    IHttpClientEvents httpClientEvents,
                                    ISolutionManager solutionManager,
                                    IPackageRestoreManager packageRestoreManager,
                                    RestartRequestBar restartRequestBar)
        {
            IVsPackageManager packageManager = packageManagerFactory.CreatePackageManagerToManageInstalledPackages();

            IPackageRepository localRepository;

            // we need different sets of providers depending on whether the dialog is open for solution or a project
            OnlineProvider onlineProvider;
            InstalledProvider installedProvider;
            UpdatesProvider updatesProvider;

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
                    solutionManager,
                    packageRestoreManager);

                updatesProvider = new SolutionUpdatesProvider(
                    localRepository,
                    Resources,
                    packageRepositoryFactory,
                    packageSourceProvider,
                    packageManagerFactory,
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
                    solutionManager,
                    packageRestoreManager);

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
            }

            explorer.Providers.Add(installedProvider);
            explorer.Providers.Add(onlineProvider);
            explorer.Providers.Add(updatesProvider);

            installedProvider.IncludePrerelease =
                onlineProvider.IncludePrerelease =
                updatesProvider.IncludePrerelease = _providerSettings.IncludePrereleasePackages;

            installedProvider.ExecuteCompletedCallback =
                onlineProvider.ExecuteCompletedCallback =
                updatesProvider.ExecuteCompletedCallback = restartRequestBar.CheckForUnsuccessfulUninstall;

            Loaded += (o, e) => restartRequestBar.CheckForUnsuccessfulUninstall();
        }

        private void UpdateSelectedProvider(int selectedProvider)
        {
            // update the selected provider
            selectedProvider = Math.Min(explorer.Providers.Count - 1, selectedProvider);
            selectedProvider = Math.Max(selectedProvider, 0);
            explorer.SelectedProvider = explorer.Providers[selectedProvider];
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't care about exception handling here.")]
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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't care about exception handling here.")]
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
                    provider.CloseProgressWindow();
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static void OnActivated(Project project)
        {
            var window = new PackageManagerWindow(project);
            try
            {
                window.ShowModal();
            }
            catch (Exception exception)
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
            PackagesTreeNodeBase oldNode = e.OldValue as PackagesTreeNodeBase;
            if (oldNode != null)
            {
                // notify the previously selected node that it was closed.
                oldNode.OnClosed();
            }

            PackagesTreeNodeBase newNode = e.NewValue as PackagesTreeNodeBase;
            if (newNode != null)
            {
                // notify the selected node that it is opened.
                newNode.OnOpened();
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
            foreach (PackagesProviderBase provider in explorer.Providers)
            {
                // give each provider a chance to clean up itself
                provider.Dispose();
            }

            explorer.Providers.Clear();

            // flush output messages to the Output console at once when the dialog is closed.
            _smartOutputConsoleProvider.Flush();

            _updateAllUIService.DisposeElement();

            if (_httpClientEvents != null)
            {
                _httpClientEvents.SendingRequest -= OnSendingRequest;
            }

            RemoveUpdateBar();
            RemoveRestoreBar();

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

        private void InsertUpdateAllButton(IUpdateAllUIService updateAllUIService)
        {
            Grid grid = LogicalTreeHelper.FindLogicalNode(explorer, "resGrid") as Grid;
            if (grid != null && grid.Children.Count > 0)
            {
                ListView listView = grid.FindDescendant<ListView>();
                if (listView != null)
                {
                    Grid firstGrid = (Grid)listView.Parent;
                    firstGrid.Children.Remove(listView);

                    var newGrid = new Grid
                    {
                        Margin = listView.Margin
                    };

                    newGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    newGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                    var updateAllContainer = updateAllUIService.CreateUIElement();
                    updateAllContainer.Margin = new Thickness(5, 2, 0, 5);
                    updateAllContainer.UpdateInvoked += OnUpdateButtonClick;
                    newGrid.Children.Add(updateAllContainer);

                    listView.Margin = new Thickness();
                    Grid.SetRow(listView, 1);
                    newGrid.Children.Add(listView);

                    firstGrid.Children.Insert(0, newGrid);
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
            if (combo.SelectedIndex == -1)
            {
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

        private static UIElement FindChildElementByNameOrType(Grid parent, string childName, Type childType)
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

                _updateAllUIService.Hide();
            }
            else
            {
                _prereleaseComboBox.Visibility = Visibility.Collapsed;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't care about exception handling here.")]
        private void OnUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            var provider = explorer.SelectedProvider as PackagesProviderBase;
            if (provider != null)
            {
                try
                {
                    provider.Execute(item: null);
                }
                catch (Exception exception)
                {
                    MessageHelper.ShowErrorMessage(exception, NuGet.Dialog.Resources.Dialog_MessageBoxTitle);
                    provider.CloseProgressWindow();
                    ExceptionHelper.WriteToActivityLog(exception);
                }
            }
        }

        private void OnSendingRequest(object sender, WebRequestEventArgs e)
        {
            HttpUtility.SetUserAgent(
                e.Request,
                _activeProject == null ? _dialogForSolutionUserAgent.Value : _dialogUserAgent.Value,
                _projectGuids);
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

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
#if !VS10
            var searchControlParent = explorer.SearchControlParent as DependencyObject;
#else
            var searchControlParent = explorer;
#endif
            var element = (TextBox)searchControlParent.FindDescendant<TextBox>();
            if (element != null && !String.IsNullOrEmpty(_searchText))
            {
                var selectedProvider = explorer.SelectedProvider as PackagesProviderBase;
                selectedProvider.SuppressLoad = false;
                element.Text = _searchText;
            }
        }
    }
}