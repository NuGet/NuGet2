using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;

namespace NuGet.VisualStudio {
    public static class MessageHelper {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        public static void ShowWarningMessage(string message, string title) {
            VsShellUtilities.ShowMessageBox(
               ServiceLocator.GetInstance<IServiceProvider>(),
               message,
               title,
               OLEMSGICON.OLEMSGICON_WARNING,
               OLEMSGBUTTON.OLEMSGBUTTON_OK,
               OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        public static void ShowInfoMessage(string message, string title) {
            VsShellUtilities.ShowMessageBox(
               ServiceLocator.GetInstance<IServiceProvider>(),
               message,
               title,
               OLEMSGICON.OLEMSGICON_INFO,
               OLEMSGBUTTON.OLEMSGBUTTON_OK,
               OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public static void ShowErrorMessage(Exception exception, string title) {
            ShowErrorMessage((exception.InnerException ?? exception).Message, title);
        }

        public static void ShowErrorMessage(string message, string title ) {
            VsShellUtilities.ShowMessageBox(
                ServiceLocator.GetInstance<IServiceProvider>(),
                message,
                title,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}