using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ExtensionManager.UI;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using Window = System.Windows.Window;

namespace NuGet.VisualStudio {

    [Export(typeof(IProductUpdateService))]
    internal class ProductUpdateService : IProductUpdateService {
        private readonly IVsUIShell _vsUIShell;
        private readonly IProductUpdateSettings _productUpdateSettings;
        private IUpdateWorker _updateWorker;
        private DTE _dte;

        private bool _updateDeclined;
        private bool _updateAccepted;
        private bool _hasCheckedUpdate;

        public ProductUpdateService() :
            this(ServiceLocator.GetGlobalService<SVsUIShell, IVsUIShell>(),
                 ServiceLocator.GetInstance<IProductUpdateSettings>(),
                 ServiceLocator.GetInstance<DTE>()) {
        }

        public ProductUpdateService(
            IVsUIShell vsUIShell,
            IProductUpdateSettings productUpdateSettings,
            DTE dte) {
            if (productUpdateSettings == null) {
                throw new ArgumentNullException("productUpdateSettings");
            }

            _dte = dte;
            _vsUIShell = vsUIShell;
            _productUpdateSettings = productUpdateSettings;
        }

        public event EventHandler<ProductUpdateAvailableEventArgs> UpdateAvailable = delegate { };

        private IUpdateWorker UpdateWorker {
            get {
                if (_updateWorker == null) {
                    if (VsVersionHelper.IsVisualStudio2010) {
                        _updateWorker = new VS2010UpdateWorker();
                    }
                    else {
                        _updateWorker = new NullUpdateWorker();
                    }
                }

                return _updateWorker;
            }
        }

        public void CheckForAvailableUpdateAsync() {
            if (_hasCheckedUpdate || _updateDeclined || _updateAccepted || !_productUpdateSettings.ShouldCheckForUpdate) {
                return;
            }

            _hasCheckedUpdate = true;

            Task.Factory.StartNew(() => {
                try {
                    Version installedVersion, newVersion;
                    if (UpdateWorker.CheckForUpdate(out installedVersion, out newVersion)) {
                        RaiseUpdateEvent(new ProductUpdateAvailableEventArgs(installedVersion, newVersion));
                    }
                }
                catch {
                    // Swallow all exceptions. We don't want to take down vs, if the VS extension
                    // gallery happens to be down.
                }
            });
        }

        private void RaiseUpdateEvent(ProductUpdateAvailableEventArgs args) {
            UpdateAvailable(this, args);
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
            if (_vsUIShell != null) {
                // first, bring up the extension manager.
                Guid toolsGroupGuid = VSConstants.VsStd2010;
                const int extensionManagerCommandId = 0xBB8;
                object pvaIn = null;
                _vsUIShell.PostExecCommand(ref toolsGroupGuid, extensionManagerCommandId, 0, ref pvaIn);

                // The Extension Manager dialog may take a while to load. Use dispatcher timer to poll it until it shows up.
                DispatcherTimer timer = new DispatcherTimer() {
                    Interval = TimeSpan.FromMilliseconds(100),
                    Tag = 0     // store the number of polls completed
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
                else {
                    // assume the Extension Manager dialog is not available, open the visual studio gallery page of nuget
                    const string NuGetGalleryPage = "http://go.microsoft.com/fwlink/?LinkID=223391";
                    System.Diagnostics.Process.Start(NuGetGalleryPage);
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