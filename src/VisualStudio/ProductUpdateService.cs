using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace NuGet.VisualStudio {

    [Export(typeof(IProductUpdateService))]
    internal class ProductUpdateService : IProductUpdateService {

        private static readonly Guid ExtensionManagerCommandGuid = new Guid("{5dd0bb59-7076-4c59-88d3-de36931f63f0}");
        private const int ExtensionManagerCommandId = (int)0xBB8;
        
        private bool _updateDeclined;

        public event EventHandler<ProductUpdateAvailableEventArgs> UpdateAvailable;

        public void CheckForAvailableUpdateAsync() {
            if (_updateDeclined) {
                return;
            }

            // TODO (Fowler): Do the actual check here
            // Must do this on background thread.
            Task.Factory.StartNew(() => {
                Thread.Sleep(2000);

                RaiseUpdateEvent(new ProductUpdateAvailableEventArgs(new Version("1.3"), new Version("1.4")));
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

            IMenuCommandService mcs = ServiceLocator.GetInstance<IMenuCommandService>();
            if (mcs != null) {
                CommandID extensionManagerCommand = new CommandID(ExtensionManagerCommandGuid, ExtensionManagerCommandId);
                mcs.GlobalInvoke(extensionManagerCommand);
            }
        }

        public void DeclineUpdate() {
            _updateDeclined = true;
        }
    }
}