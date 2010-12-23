using System;
using System.ComponentModel.Composition;

namespace NuGet.Dialog.PackageManagerUI {

    [Export(typeof(IProgressWindowOpener))]
    public sealed class ProgressWindowOpener : IProgressWindowOpener {
        private ProgressDialog _currentWindow;

        public bool? ShowModal(string title) {
            _currentWindow = new ProgressDialog();
            _currentWindow.Title = title;
            return _currentWindow.ShowModal();
        }

        public bool IsOpen {
            get {
                return _currentWindow != null && _currentWindow.IsVisible;
            }
        }

        public bool Close() {
            if (IsOpen) {
                _currentWindow.ForceClose();
                return true;
            }
            else {
                return false;
            }
        }

        public void SetCompleted() {
            if (IsOpen) {
                _currentWindow.SetCompleted();
            }
        }
    }
}
