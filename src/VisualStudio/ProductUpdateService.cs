using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Linq;
using System.Security.Principal;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.ExtensionManager.UI;
using Task = System.Threading.Tasks.Task;

namespace NuGet.VisualStudio {
    [Export(typeof(IProductUpdateService))]
    internal class ProductUpdateService : IProductUpdateService {
        private static readonly Guid ExtensionManagerCommandGuid = new Guid("{5dd0bb59-7076-4c59-88d3-de36931f63f0}");
        private const string NuGetVSIXId = "NuPackToolsVsix.Microsoft.67e54e40-0ae3-42c5-a949-fddf5739e7a5";
        private const int ExtensionManagerCommandId = (int)0xBB8;
        private readonly IMenuCommandService _menuCommandService;
        private readonly IVsExtensionRepository _extensionRepository;

        private bool _updateDeclined;
        private bool _updateAccepted;

        public ProductUpdateService() :
            this(ServiceLocator.GetInstance<IMenuCommandService>(),
                 ServiceLocator.GetGlobalService<SVsExtensionRepository, IVsExtensionRepository>()) {
        }

        public ProductUpdateService(IMenuCommandService menuCommandService, IVsExtensionRepository extensionRepository) {
            _menuCommandService = menuCommandService;
            _extensionRepository = extensionRepository;
        }

        public event EventHandler<ProductUpdateAvailableEventArgs> UpdateAvailable;

        public void CheckForAvailableUpdateAsync() {
            if (_updateDeclined || _updateAccepted) {
                return;
            }

            // If the user isn't admin then they can't perform update check.
            if (!IsElevated()) {
                _updateDeclined = true;
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

        private bool IsElevated() {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
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

            if (_menuCommandService != null) {
                CommandID extensionManagerCommand = new CommandID(ExtensionManagerCommandGuid, ExtensionManagerCommandId);
                _menuCommandService.GlobalInvoke(extensionManagerCommand);
            }
        }

        public void DeclineUpdate() {
            _updateDeclined = true;
        }
    }
}