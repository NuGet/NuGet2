using System;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    public static class ProgressDialogHelper
    {
        private static IVsThreadedWaitDialogFactory _threadedDialogFactory;

        public static T DoWorkWhileShowingProgress<T>(Func<T> work, string title, string message)
        {
            if (_threadedDialogFactory == null)
            {
                _threadedDialogFactory = ServiceLocator.GetGlobalService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>();
            }

            IVsThreadedWaitDialog2 waitDialog;
            _threadedDialogFactory.CreateInstance(out waitDialog);
            try
            {
                waitDialog.StartWaitDialog(
                    title,
                    message,
                    String.Empty,
                    varStatusBmpAnim: null,
                    szStatusBarText: null,
                    iDelayToShowDialog: 0,
                    fIsCancelable: false,
                    fShowMarqueeProgress: true);

                return work();
            }
            catch (Exception exception)
            {
                ExceptionHelper.WriteToActivityLog(exception);
                return default(T);
            }
            finally
            {
                int canceled;
                waitDialog.EndWaitDialog(out canceled);
            }
        }
    }
}
