using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using System.Windows.Threading;

namespace NuGet.Dialog.PackageManagerUI {

    [Export(typeof(IProgressWindowOpener))]
    public sealed class ProgressWindowOpener : IProgressWindowOpener {
        private ProgressDialog _currentWindow;
        private readonly Dispatcher _uiDispatcher;

        private Lazy<DispatcherTimer> _showTimer;

        public ProgressWindowOpener() {
            _uiDispatcher = Dispatcher.CurrentDispatcher;

            _showTimer = new Lazy<DispatcherTimer>(() => {
                var timer = new DispatcherTimer {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                timer.Tick += new EventHandler(OnShowTimerTick);
                return timer;
            });
        }

        private bool IsPendingShow {
            get {
                return _showTimer.IsValueCreated && _showTimer.Value.IsEnabled;
            }
        }

        private void CancelPendingShow() {
            if (_showTimer.IsValueCreated) {
                _showTimer.Value.Stop();
            }
        }

        private void OnShowTimerTick(object sender, EventArgs e) {
            CancelPendingShow();

            if (!_currentWindow.IsVisible) {
                if (_currentWindow.IsLoaded) {
                    _currentWindow.ShowDialog();
                }
                else {
                    _currentWindow.ShowModal();
                }
            }
        }

        /// <summary>
        /// Show the progress window with the specified title, after a delay of 500ms.
        /// </summary>
        /// <param name="title">The window title</param>
        /// <remarks>
        /// This method can be called from worker thread.
        /// </remarks>
        public void Show(string title) {
            if (!_uiDispatcher.CheckAccess()) {
                // must use BeginInvoke() here to avoid blocking the worker thread
                _uiDispatcher.BeginInvoke(new Action<string>(Show), title);
                return;
            }

            if (!IsPendingShow) {
                if (_currentWindow == null) {
                    _currentWindow = new ProgressDialog();
                    _currentWindow.Closed += OnWindowClosed;
                }
                _currentWindow.Title = title;

                _showTimer.Value.Start();
            }
        }

        private void OnWindowClosed(object sender, EventArgs e) {
            if (_currentWindow != null) {
                _currentWindow.Closed -= OnWindowClosed;
                _currentWindow = null;
            }
        }

        /// <summary>
        /// Hide the progress window if it is open.
        /// </summary>
        /// <remarks>
        /// This method can be called from worker thread.
        /// </remarks>
        public void Hide() {
            if (!_uiDispatcher.CheckAccess()) {
                // must use BeginInvoke() here to avoid blocking the worker thread
                _uiDispatcher.BeginInvoke(new Action(Hide));
                return;
            }

            if (IsOpen) {
                CancelPendingShow();
                _currentWindow.Hide();
            }
        }

        /// <summary>
        /// This property is only logical. The dialog may not be actually visible even if 
        /// the property returns true, due to the delay in showing.
        /// </summary>
        public bool IsOpen {
            get {
                return _currentWindow != null && (_currentWindow.IsVisible || IsPendingShow);
            }
        }

        public bool Close() {
            if (IsOpen) {
                CancelPendingShow();

                _currentWindow.ForceClose();
                _currentWindow = null;
                return true;
            }
            else {
                return false;
            }
        }

        public void SetCompleted(bool successful) {
            if (IsOpen) {
                if (successful) {
                    // if successful, we are going to close the dialog automatically.
                    // so cancel any pending show operation.
                    CancelPendingShow();
                }

                _currentWindow.SetCompleted(successful);
            }
        }

        /// <summary>
        /// Add a logging message to the progress window.
        /// </summary>
        /// <remarks>
        /// This method can be called from worker thread.
        /// </remarks>
        public void AddMessage(MessageLevel level, string message) {
            if (!_uiDispatcher.CheckAccess()) {
                _uiDispatcher.BeginInvoke(new Action<MessageLevel, string>(AddMessage), level, message);
                return;
            }

            if (IsOpen) {
                Brush messageBrush;

                // select message color based on MessageLevel value.
                // these colors match the colors in the console, which are set in MyHostUI.cs
                switch (level) {
                    case MessageLevel.Debug:
                        messageBrush = Brushes.DarkGray;
                        break;

                    case MessageLevel.Error:
                        messageBrush = Brushes.Red;
                        break;

                    case MessageLevel.Warning:
                        messageBrush = Brushes.Magenta;
                        break;

                    default:
                        messageBrush = Brushes.Black;
                        break;
                }

                _currentWindow.AddMessage(message, messageBrush);
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public void ShowProgress(string operation, int percentComplete) {
            if (!_uiDispatcher.CheckAccess()) {
                _uiDispatcher.BeginInvoke(new Action<string, int>(ShowProgress), operation, percentComplete);
                return;
            }

            if (operation == null) {
                throw new ArgumentNullException("operation");
            }

            if (IsOpen) {
                if (percentComplete < 0) {
                    percentComplete = 0;
                }
                else if (percentComplete > 100) {
                    percentComplete = 100;
                }

                _currentWindow.ShowProgress(operation, percentComplete);
            }
        }
    }
}