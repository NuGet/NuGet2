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
                NuGet.Dialog.Resources.Dialog_MessageBoxTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        public static void ShowWarningMessage(string message) {
            ShowWarningMessage(message, NuGet.Dialog.Resources.Dialog_MessageBoxTitle);
        }

        public static void ShowWarningMessage(string message, string title) {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

    }
}
