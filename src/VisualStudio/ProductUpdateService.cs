using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.VisualStudio {

    [Export(typeof(IProductUpdateService))]
    internal class ProductUpdateService : IProductUpdateService {

        private bool _updateDeclined;

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

            // TODO (Fowler): Do the actual update
        }

        public void DeclineUpdate() {
            _updateDeclined = true;
        }

        public event EventHandler<ProductUpdateAvailableEventArgs> UpdateAvailable;
    }
}