using System;
using System.ComponentModel.Composition;
using System.Windows;
using PackageExplorerViewModel.Types;

namespace PackageExplorer {

    [Export(typeof(IMessageBox))]
    public class MessageBoxService : IMessageBox {

        public MessageBoxService() {
        }

        public bool Confirm(string message) {
            MessageBoxResult result = MessageBox.Show(
                message, 
                Resources.Resources.Dialog_Title, 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }


        public bool? ConfirmWithCancel(string message) {
            MessageBoxResult result = MessageBox.Show(
                message, 
                Resources.Resources.Dialog_Title, 
                MessageBoxButton.YesNoCancel, 
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) {
                return null;
            }
            else {
                return result == MessageBoxResult.Yes;
            }
        }

        public void Show(string message, MessageLevel messageLevel) {
            MessageBoxImage image;
            switch (messageLevel) {
                case MessageLevel.Error:
                    image = MessageBoxImage.Error;
                    break;

                case MessageLevel.Information:
                    image = MessageBoxImage.Information;
                    break;

                case MessageLevel.Warning:
                    image = MessageBoxImage.Warning;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("messageLevel");
            }

            MessageBox.Show(
                message,
                Resources.Resources.Dialog_Title,
                MessageBoxButton.OK,
                image);
        }
    }
}
