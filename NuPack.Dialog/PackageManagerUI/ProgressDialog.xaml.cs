using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;

namespace NuGet.Dialog.PackageManagerUI {
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : DialogWindow {
        public ProgressDialog() {
            InitializeComponent();
        }

        internal void SetCompleted(bool successful) {
            OkButton.IsEnabled = true;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = ProgressBar.Maximum;
            StatusText.Text = successful ? NuGet.Dialog.Resources.Dialog_OperationSucceeded : NuGet.Dialog.Resources.Dialog_OperationFailed;
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

        public void AddMessage(string message, Brush messageColor) {
            Paragraph paragraph = null;

            if (MessagePane.Document == null) {
                MessagePane.Document = new FlowDocument();
                paragraph = new Paragraph();
                MessagePane.Document.Blocks.Add(paragraph);
            }
            else {
                paragraph = (Paragraph)MessagePane.Document.Blocks.LastBlock;
            }

            var run = new Run(message) {
                Foreground = messageColor 
            };

            if (paragraph.Inlines.Count > 0) {
                paragraph.Inlines.Add(new LineBreak());
            }
            paragraph.Inlines.Add(run);
            MessagePane.ScrollToEnd();
        }
    }
}