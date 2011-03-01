using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
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

        protected override void OnSourceInitialized(System.EventArgs e) {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;
            NativeMethods.SetWindowLong(
                hwnd, 
                NativeMethods.GWL_STYLE,
                NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE) & ~NativeMethods.WS_SYSMENU);
        }

        internal void SetCompleted(bool successful) {
            if (successful) {
                ForceClose();
            }
            else {
                OkButton.IsEnabled = true;
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = ProgressBar.Maximum;
                StatusText.Text = NuGet.Dialog.Resources.Dialog_OperationFailed;
            }
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

            // delay creating the FlowDocument for the RichTextBox
            // the FlowDocument will contain a single Paragraph, which
            // contains all the logging messages.
            if (MessagePane.Document == null) {
                MessagePane.Document = new FlowDocument();
                paragraph = new Paragraph();
                MessagePane.Document.Blocks.Add(paragraph);
            }
            else {
                // if the FlowDocument has been created before, retrieve 
                // the last paragraph from it.
                paragraph = (Paragraph)MessagePane.Document.Blocks.LastBlock;
            }

            // each message is reprepseted by a Run element
            var run = new Run(message) {
                Foreground = messageColor 
            };

            // if the paragraph is non-empty, add a line break before the new message
            if (paragraph.Inlines.Count > 0) {
                paragraph.Inlines.Add(new LineBreak());
            }

            paragraph.Inlines.Add(run);

            // scroll to the end to show the latest message
            MessagePane.ScrollToEnd();
        }

        public void ShowProgress(string operation, int percentComplete) {
            if (percentComplete == ProgressBar.Maximum) {
                // the progress complete, reverts back to indeterminate state
                ProgressBar.IsIndeterminate = true;
                StatusText.Text = Title;
            }
            else {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = percentComplete;
                StatusText.Text = operation;
            }
        }
    }
}