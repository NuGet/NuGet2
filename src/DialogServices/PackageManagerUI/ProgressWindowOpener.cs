using System;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;

namespace NuGet.Dialog.PackageManagerUI
{
    public sealed class ProgressWindowOpener : IProgressWindowOpener
    {
        private IVsThreadedWaitDialog2 _currentWindow;
        private readonly IVsThreadedWaitDialogFactory _waitDialogFactory;
        private bool _cancelable;
        private string _title;

        public ProgressWindowOpener()
            : this(ServiceLocator.GetGlobalService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>())
        {
        }

        internal ProgressWindowOpener(IVsThreadedWaitDialogFactory waitDialogFactory)
        {
            if (waitDialogFactory == null)
            {
                throw new ArgumentNullException("waitDialogFactory");
            }
            _waitDialogFactory = waitDialogFactory;
        }

        public bool IsOpen
        {
            get
            {
                return _currentWindow != null;
            }
        }

        /// <summary>
        /// Show the progress window with the specified title and whether to show the Cancel button.
        /// </summary>
        /// <param name="title">The window title</param>
        /// <remarks>
        /// This method can be called from worker thread.
        /// </remarks>
        public void Show(string title, bool cancelable)
        {
            if (_currentWindow == null)
            {
                lock (_waitDialogFactory)
                {
                    if (_currentWindow == null)
                    {
                        _waitDialogFactory.CreateInstance(out _currentWindow);
                        _cancelable = cancelable;
                        _title = title;
                        ThreadHelper.Generic.Invoke(() =>
                            {
                                _currentWindow.StartWaitDialog(
                                    "Manage NuGet Packages",
                                    title,
                                    null,
                                    varStatusBmpAnim: null,
                                    szStatusBarText: null,
                                    iDelayToShowDialog: 0,
                                    fIsCancelable: _cancelable,
                                    fShowMarqueeProgress: true);
                            }
                        );
                    }
                }
            }
        }

        public bool Close()
        {
            if (_currentWindow != null)
            {
                lock (_waitDialogFactory)
                {
                    if (_currentWindow != null)
                    {
                        ThreadHelper.Generic.Invoke(() =>
                            {
                                int canceled;
                                _currentWindow.EndWaitDialog(out canceled);
                            });

                        _currentWindow = null;
                        _title = null;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Add a message to the progress window.
        /// </summary>
        /// <remarks>
        /// This method can be called from worker thread.
        /// </remarks>
        public bool UpdateMessageAndQueryStatus(string message)
        {
            if (IsOpen)
            {
                return ThreadHelper.Generic.Invoke(() =>
                {
                    bool canceled;
                    _currentWindow.UpdateProgress(
                        _title,
                        message,
                        String.Empty,
                        0,
                        0,
                        fDisableCancel: !_cancelable,
                        pfCanceled: out canceled);

                    return canceled;
                });
            }

            return false;
        }
    }
}