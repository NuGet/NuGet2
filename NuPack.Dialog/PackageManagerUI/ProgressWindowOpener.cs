using System;
using System.ComponentModel.Composition;
using System.Windows.Threading;

namespace NuGet.Dialog.PackageManagerUI {

    [Export(typeof(IProgressWindowOpener))]
    public sealed class ProgressWindowOpener : IProgressWindowOpener {
        private ProgressDialog _currentWindow;
        private readonly Dispatcher _uiDispatcher;

        public ProgressWindowOpener() {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Show(string title) {
            if (!_uiDispatcher.CheckAccess()) {
                // must use BeginInvoke() here to avoid blocking the worker thread
                _uiDispatcher.BeginInvoke(new Action<string>(Show), title);
                return;
            }

            if (IsOpen) {
                // if the window is hidden, just re-show it instead of creating a new window instance
                _currentWindow.Title = title;
                _currentWindow.ShowDialog();
            }
            else {
                _currentWindow = new ProgressDialog();
                _currentWindow.Title = title;
                _currentWindow.Closed += OnWindowClosed;

                _currentWindow.ShowModal();
            }
        }

        private void OnWindowClosed(object sender, EventArgs e) {
            if (_currentWindow != null) {
                _currentWindow.Closed -= OnWindowClosed;
                _currentWindow = null;
            }
        }

        public void Hide() {
            if (!_uiDispatcher.CheckAccess()) {
                // must use BeginInvoke() here to avoid blocking the worker thread
                _uiDispatcher.BeginInvoke(new Action(Hide));
                return;
            }

            if (IsOpen) {
                _currentWindow.Hide();
            }
        }

        public bool IsOpen {
            get {
                return _currentWindow != null;
            }
        }

        public bool Close() {
            if (IsOpen) {
                _currentWindow.ForceClose();
                _currentWindow = null;
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
