using System;
using System.Windows;

namespace NuGet.Dialog.PackageManagerUI {

    internal static class MessageHelper {

        public static void ShowErrorMessage(Exception exception) {
            MessageBox.Show(
                (exception.InnerException ?? exception).Message,
                NuGet.Dialog.Resources.Dialog_MessageBoxTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        public static void ShowWarningMessage(string message) {
            MessageBox.Show(
                message, 
                NuGet.Dialog.Resources.Dialog_MessageBoxTitle, 
                MessageBoxButton.OK, 
                MessageBoxImage.Warning);
        }


    }
}
