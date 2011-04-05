using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Automation;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.ExtensionManager.UI;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;
using Thread = System.Threading.Thread;

namespace NuGet.VisualStudio {

    [Export(typeof(IProductUpdateService))]
    internal class ProductUpdateService : IProductUpdateService {

        private static readonly object _showUpdatesLock = new object();
        private const string NuGetVSIXId = "NuPackToolsVsix.Microsoft.67e54e40-0ae3-42c5-a949-fddf5739e7a5";
        private readonly IVsExtensionRepository _extensionRepository;
        private readonly IVsUIShell _vsUIShell;
        private readonly IProductUpdateSettings _productUpdateSettings;

        private bool _updateDeclined;
        private bool _updateAccepted;

        public ProductUpdateService() :
            this(ServiceLocator.GetGlobalService<SVsExtensionRepository, IVsExtensionRepository>(),
                 ServiceLocator.GetGlobalService<SVsUIShell, IVsUIShell>(),
                 ServiceLocator.GetInstance<IProductUpdateSettings>()) {
        }

        public ProductUpdateService(IVsExtensionRepository extensionRepository, IVsUIShell vsUIShell, IProductUpdateSettings productUpdateSettings) {
            if (productUpdateSettings == null) {
                throw new ArgumentNullException("productUpdateSettings");
            }

            _extensionRepository = extensionRepository;
            _vsUIShell = vsUIShell;
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
                    // Get the current NuGet version
                    Version version = typeof(ProductUpdateService).Assembly.GetName().Version;

                    // If we're running an older version then update
                    if (nugetVsix != null && nugetVsix.NonNullVsixVersion > version) {
                        RaiseUpdateEvent(new ProductUpdateAvailableEventArgs(version, nugetVsix.NonNullVsixVersion));
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
            Task.Factory.StartNew(() => {
                lock (_showUpdatesLock) {
                    try {
                        // check if the Extension Manager window is already open
                        AutomationElement extensionManagerWindow = GetExtensionManagerWindow();
                        if (extensionManagerWindow == null) {
                            // if not, invoke the command to bring it up
                            Guid pguidCmdGroup = VSConstants.VsStd2010;
                            object pvaIn = null;
                            _vsUIShell.PostExecCommand(ref pguidCmdGroup, 0xbb8, 0, ref pvaIn);
                        }

                        // it will take a brief moment for the window to show up, polling until it does
                        while (extensionManagerWindow == null) {
                            extensionManagerWindow = GetExtensionManagerWindow();
                            Thread.Sleep(100);
                        }

                        // search for the Updates tab on the window and select it through Automation
                        AutomationElement updatesTab = FindUpdateProviderTab(extensionManagerWindow);
                        if (updatesTab != null) {
                            (updatesTab.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern).Select();
                        }
                    }
                    catch (Exception) {
                    }
                }
            });
        }

        private AutomationElement GetExtensionManagerWindow() {
            return AutomationElement.FromHandle(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle).
                FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "ExtensionManagerDialog"));
        }

        private AutomationElement FindUpdateProviderTab(AutomationElement extensionManagerWindow) {
            // look for the treeView element hosting the providers tab
            AutomationElement treeViewElement = extensionManagerWindow.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "ProvidersUid"));
            if (treeViewElement == null) {
                return null;
            }

            // pick the Updates tab among the children TreeViewItem instances
            return treeViewElement.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem)).
                Cast<AutomationElement>().
                FirstOrDefault<AutomationElement>(x => x.Current.AutomationId.StartsWith("Updates"));
        }
    }
}