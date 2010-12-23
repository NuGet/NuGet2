using System;
using Microsoft.VisualStudio.PlatformUI;

namespace NuGet.Dialog.PackageManagerUI {
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : DialogWindow {
        public ProgressDialog() {
            InitializeComponent();
        }

        internal void SetCompleted() {
            OkButton.IsEnabled = true;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = ProgressBar.Maximum;
            StatusText.Text = NuGet.Dialog.Resources.Dialog_OperationCompleted;
        }

        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e) {
            DialogResult = true;
        }

        private void DialogWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            // do not allow user to close the form when the operation has not completed
            e.Cancel = !OkButton.IsEnabled;
        }

        public void ForceClose() {
            OkButton.IsEnabled = true;
            Close();
        }
    }
}
