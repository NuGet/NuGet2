using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using NuGet.VisualStudio;

namespace NuGet.Dialog.PackageManagerUI
{
    public sealed class ProgressWindowOpener : IProgressWindowOpener
    {
        private static readonly TimeSpan DelayInterval = TimeSpan.FromMilliseconds(500);

        private ProgressDialog _currentWindow;
        private readonly Dispatcher _uiDispatcher;
        private DateTime _lastShowTime = DateTime.MinValue;

        private readonly Lazy<DispatcherTimer> _closeTimer;
        private readonly Lazy<DispatcherTimer> _showTimer;

        public ProgressWindowOpener()
        {
            _uiDispatcher = Dispatcher.CurrentDispatcher;

            _showTimer = new Lazy<DispatcherTimer>(() =>
            {
                var timer = new DispatcherTimer
                {
                    Interval = DelayInterval
                };
                timer.Tick += new EventHandler(OnShowTimerTick);
                return timer;
            });

            _closeTimer = new Lazy<DispatcherTimer>(() =>
            {
                var timer = new DispatcherTimer();
                timer.Tick += new EventHandler(OnCloseTimerTick);
                return timer;
            });
        }

        public event EventHandler UpgradeNuGetRequested = delegate { };

        private DispatcherTimer CloseTimer
        {
            get
            {
                return _closeTimer.Value;
            }
        }

        private bool IsPendingShow
        {
            get
            {
                return _showTimer.IsValueCreated && _showTimer.Value.IsEnabled;
            }
        }

        private void CancelPendingShow()
        {
            if (_showTimer.IsValueCreated)
            {
                _showTimer.Value.Stop();
            }
        }

        private void OnShowTimerTick(object sender, EventArgs e)
        {
            CancelPendingShow();

            if (!_currentWindow.IsVisible)
            {
                _currentWindow.Show();
                _lastShowTime = DateTime.Now;
            }
        }

        private void CancelPendingClose()
        {
            if (_closeTimer.IsValueCreated)
            {
                CloseTimer.Stop();
            }
        }

        private void OnCloseTimerTick(object sender, EventArgs e)
        {
            CancelPendingClose();
            HandleClose(hideOnly: (bool)CloseTimer.Tag);
        }

        /// <summary>
        /// Show the progress window with the specified title, after a delay of 500ms.
        /// </summary>
        /// <param name="title">The window title</param>
        /// <remarks>
        /// This method can be called from worker thread.
        /// </remarks>
        public void Show(string title, Window owner)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                // must use BeginInvoke() here to avoid blocking the worker thread
                _uiDispatcher.BeginInvoke(new Action<string, Window>(Show), DispatcherPriority.Send, title, owner);
                return;
            }

            if (!IsPendingShow)
            {
                CancelPendingClose();

                if (_currentWindow == null)
                {
                    NuGetEventTrigger.Instance.TriggerEvent(NuGetEvent.ProgressDialogBegin);
                    _currentWindow = new ProgressDialog() { Owner = owner };
                    _currentWindow.Closed += OnWindowClosed;
                }
                _currentWindow.Title = title;

                _showTimer.Value.Start();
            }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            if (_currentWindow != null)
            {
                bool upgradeRequested = _currentWindow.UpgradeNuGetRequested;
                
                _currentWindow.Closed -= OnWindowClosed;
                _currentWindow = null;
                NuGetEventTrigger.Instance.TriggerEvent(NuGetEvent.ProgressDialogEnd);

                if (upgradeRequested)
                {
                    UpgradeNuGetRequested(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Hide the progress window if it is open.
        /// </summary>
        /// <remarks>
        /// This method can be called from worker thread.
        /// </remarks>
        public void Hide()
        {
            if (!_uiDispatcher.CheckAccess())
            {
                // must use BeginInvoke() here to avoid blocking the worker thread
                _uiDispatcher.BeginInvoke(new Action(Hide), DispatcherPriority.Send);
                return;
            }

            if (IsOpen)
            {
                CancelPendingShow();
                HandleClose(hideOnly: true);
            }
        }

        /// <summary>
        /// This property is only logical. The dialog may not be actually visible even if 
        /// the property returns true, due to the delay in showing.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return _currentWindow != null && (_currentWindow.IsVisible || IsPendingShow);
            }
        }

        public bool Close()
        {
            if (IsOpen)
            {
                CancelPendingShow();
                HandleClose(hideOnly: false);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void HandleClose(bool hideOnly)
        {
            TimeSpan elapsed = DateTime.Now - _lastShowTime;
            if (elapsed >= DelayInterval)
            {
                // if the dialog has been shown for more than 500ms, just close it
                if (_currentWindow != null && _currentWindow.IsVisible)
                {
                    if (hideOnly)
                    {
                        _currentWindow.Hide();
                    }
                    else
                    {
                        _currentWindow.ForceClose();
                    }
                }
            }
            else
            {
                CloseTimer.Tag = hideOnly;
                // otherwise, set a timer so that we close it after it has been shown for 500ms
                CloseTimer.Interval = DelayInterval - elapsed;
                CloseTimer.Start();
            }
        }

        public void SetCompleted(bool successful, bool showUpgradeNuGetButton)
        {
            if (successful)
            {
                Close();
            }
            else if (_currentWindow != null)
            {
                _currentWindow.SetErrorState(showUpgradeNuGetButton);
            }
        }

        /// <summary>
        /// Add a logging message to the progress window.
        /// </summary>
        /// <remarks>
        /// This method can be called from worker thread.
        /// </remarks>
        public void AddMessage(MessageLevel level, string message)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                _uiDispatcher.BeginInvoke(new Action<MessageLevel, string>(AddMessage), DispatcherPriority.Send, level, message);
                return;
            }

            if (IsOpen)
            {
                Brush messageBrush;

                // select message color based on MessageLevel value.
                // these colors match the colors in the console, which are set in MyHostUI.cs
                if (SystemParameters.HighContrast)
                {
                    // Use the plain System brush
                    messageBrush = SystemColors.ControlTextBrush;
                }
                else
                {
                    switch (level)
                    {
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
                }

                _currentWindow.AddMessage(message, messageBrush);
            }
        }

        public void ClearMessages()
        {
            if (!_uiDispatcher.CheckAccess())
            {
                _uiDispatcher.Invoke(new Action(ClearMessages), DispatcherPriority.Send);
                return;
            }

            if (_currentWindow != null)
            {
                _currentWindow.ClearMessages();
            }
        }

        public void ShowProgress(string operation, int percentComplete)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                _uiDispatcher.BeginInvoke(new Action<string, int>(ShowProgress), DispatcherPriority.Send, operation, percentComplete);
                return;
            }

            if (operation == null)
            {
                throw new ArgumentNullException("operation");
            }

            if (IsOpen)
            {
                if (percentComplete < 0)
                {
                    percentComplete = 0;
                }
                else if (percentComplete > 100)
                {
                    percentComplete = 100;
                }

                _currentWindow.ShowProgress(operation, percentComplete);
            }
        }
    }
}