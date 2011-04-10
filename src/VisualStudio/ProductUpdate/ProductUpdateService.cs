using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.ExtensionManager.UI;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using Task = System.Threading.Tasks.Task;

namespace NuGet.VisualStudio {

    [Export(typeof(IProductUpdateService))]
    internal class ProductUpdateService : IProductUpdateService {
        private static readonly object _showUpdatesLock = new object();
        private static readonly Guid ExtensionManagerCommandGuid = new Guid("{5dd0bb59-7076-4c59-88d3-de36931f63f0}");
        private const int ExtensionManagerCommandId = (int)0xBB8;
        private const string NuGetVSIXId = "NuPackToolsVsix.Microsoft.67e54e40-0ae3-42c5-a949-fddf5739e7a5";

        private readonly IMenuCommandService _menuCommandService;
        private readonly IVsExtensionRepository _extensionRepository;
        private readonly IVsExtensionManager _extensionManager;
        private readonly IProductUpdateSettings _productUpdateSettings;

        private bool _updateDeclined;
        private bool _updateAccepted;

        public ProductUpdateService() :
            this(ServiceLocator.GetGlobalService<SVsExtensionRepository, IVsExtensionRepository>(),
                 ServiceLocator.GetGlobalService<SVsExtensionManager, IVsExtensionManager>(),
                 ServiceLocator.GetInstance<IMenuCommandService>(),
                 ServiceLocator.GetInstance<IProductUpdateSettings>()) {
        }

        public ProductUpdateService(
            IVsExtensionRepository extensionRepository,
            IVsExtensionManager extensionManager,
            IMenuCommandService menuCommandService, 
            IProductUpdateSettings productUpdateSettings) {
            if (productUpdateSettings == null) {
                throw new ArgumentNullException("productUpdateSettings");
            }

            _menuCommandService = menuCommandService;
            _extensionRepository = extensionRepository;
            _extensionManager = extensionManager;
            _productUpdateSettings = productUpdateSettings;
        }

        public event EventHandler<ProductUpdateAvailableEventArgs> UpdateAvailable;

        public void CheckForAvailableUpdateAsync() {
            if (_updateDeclined || _updateAccepted || !_productUpdateSettings.ShouldCheckForUpdate) {
                return;
            }

            Task.Factory.StartNew(() => {
                try {
                    // Find the vsix on the vs gallery
                    VSGalleryEntry nugetVsix = _extensionRepository.CreateQuery<VSGalleryEntry>()
                                                              .Where(e => e.VsixID == NuGetVSIXId)
                                                              .AsEnumerable()
                                                              .FirstOrDefault();
                    // Get the current NuGet VSIX version
#if DEBUG
                    Version installedVersion = new Version("1.1");                    
#else
                    IInstalledExtension installedNuGet = _extensionManager.GetInstalledExtension(NuGetVSIXId);
                    Version installedVersion = installedNuGet.Header.Version;
#endif
                    // If we're running an older version then update
                    if (nugetVsix != null && nugetVsix.NonNullVsixVersion > installedVersion) {
                        RaiseUpdateEvent(new ProductUpdateAvailableEventArgs(installedVersion, nugetVsix.NonNullVsixVersion));
                    }
                }
                catch {
                    // Swallow all exceptions. We don't want to take down vs, if the VS extension
                    // gallery happens to be down.
                }
            });
        }

        private void RaiseUpdateEvent(ProductUpdateAvailableEventArgs args) {
            EventHandler<ProductUpdateAvailableEventArgs> handler = UpdateAvailable;
            if (handler != null) {
                handler(this, args);
            }
        }

        public void Update() {
            if (_updateDeclined) {
                return;
            }

            _updateAccepted = true;
            ShowUpdatesTabInExtensionManager();
        }

        public void DeclineUpdate(bool doNotRemindAgain) {
            _updateDeclined = true;

            if (doNotRemindAgain) {
                _productUpdateSettings.ShouldCheckForUpdate = false;
            }
        }

        private void ShowUpdatesTabInExtensionManager() {
            if (_menuCommandService != null) {
                // first, bring up the extension manager
                CommandID extensionManagerCommand = new CommandID(ExtensionManagerCommandGuid, ExtensionManagerCommandId);
                _menuCommandService.GlobalInvoke(extensionManagerCommand);

                // The Extension Manager dialog may take a while to load. Use dispatcher timer to poll it until it shows up.
                DispatcherTimer timer = new DispatcherTimer() {
                    Interval = TimeSpan.FromMilliseconds(100),
                    Tag = 0     // store the number of pollings completed
                };
                timer.Tick += OnTimerTick;
                timer.Start();
            }
        }

        private void OnTimerTick(object sender, EventArgs e) {
            var timer = (DispatcherTimer)sender;
            timer.Stop();

            // search through all open windows in the current application and look for the Extension Manager window
            Window extensionManager = Application.Current.Windows.OfType<ExtensionManagerWindow>().FirstOrDefault();
            if (extensionManager != null) {
                ActivateUpdatesTab(extensionManager);
            }
            else {
                // if we didn't find it, try again after 100 miliseconds
                int times = (int)timer.Tag;
                // but only try maximum of 10 times.
                if (times < 10) {
                    timer.Tag = times + 1;
                    timer.Start();
                }
            }
        }

        private static void ActivateUpdatesTab(Window extensionManager) {
            var layoutRoot = extensionManager.Content as FrameworkElement;
            if (layoutRoot != null) {
                // first, search for the Extension Explorer control inside the Extension manager window
                var explorerControl = layoutRoot.FindName("explorer") as VSExtensionsExplorerCtl;
                if (explorerControl != null) {
                    // now get the Updates provider, which should be the last one according to the SortOrder
                    var updatesProvider = explorerControl.Providers.OrderByDescending(p => p.SortOrder).FirstOrDefault();
                    if (updatesProvider != null) {
                        // Select the updates provider
                        explorerControl.SelectedProvider = updatesProvider;
                    }
                }
            }
        }
    }
}