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

                RaiseUpdateEvent();
            });
        }

        private void RaiseUpdateEvent() {
            EventHandler handler = UpdateAvailable;
            if (handler != null) {
                handler(this, EventArgs.Empty);
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

        public event EventHandler UpdateAvailable;
    }
}