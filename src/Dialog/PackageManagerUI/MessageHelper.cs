using System;
using System.Windows;

namespace NuGet.Dialog.PackageManagerUI {

    internal static class MessageHelper {

        public static void ShowErrorMessage(Exception exception) {
            ShowErrorMessage((exception.InnerException ?? exception).Message);
        }

        public static void ShowErrorMessage(string message) {
            MessageBox.Show(
                message,
                Resources.Dialog_MessageBoxTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
