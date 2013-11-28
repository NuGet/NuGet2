using System;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using System.Windows;
using NuGet.VisualStudio;

namespace NuGet.Dialog.PackageManagerUI
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : VsDialogWindow
    {
        private bool _completed;

        public ProgressDialog()
        {
            InitializeComponent();
        }

        public bool UpgradeNuGetRequested { get; private set; }

        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr hMenu = NativeMethods.GetSystemMenu(new WindowInteropHelper(this).Handle, false);
            int menuItemCount = NativeMethods.GetMenuItemCount(hMenu);
            NativeMethods.RemoveMenu(hMenu, menuItemCount - 1, NativeMethods.MF_BYPOSITION);
        }

        internal void SetErrorState(bool showUpgradeButton)
        {
            _completed = true;
            OkButton.IsEnabled = true;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = ProgressBar.Maximum;
            ProgressBar.Foreground = Brushes.Red;
            StatusText.Text = NuGet.Dialog.Resources.Dialog_OperationFailed;
            UpgradeButton.Visibility = showUpgradeButton ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            // do not allow user to close the form when the operation has not completed
            e.Cancel = !_completed;

            if (!e.Cancel)
            {
                // bug 1598: after closing the (modeless) progress dialog, focus switch to another application.
                // call Activate() here to prevent that.
                if (Owner != null)
                {
                    Owner.Activate();
                }
            }
        }

        public void ForceClose()
        {
            _completed = true;
            Close();
        }

        private void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            UpgradeNuGetRequested = true;
            ForceClose();
        }

        public void AddMessage(string message, Brush messageColor)
        {
            Paragraph paragraph = null;

            // delay creating the FlowDocument for the RichTextBox
            // the FlowDocument will contain a single Paragraph, which
            // contains all the logging messages.
            if (MessagePane.Document == null)
            {
                MessagePane.Document = new FlowDocument();
                paragraph = new Paragraph();
                MessagePane.Document.Blocks.Add(paragraph);
            }
            else
            {
                // if the FlowDocument has been created before, retrieve 
                // the last paragraph from it.
                paragraph = (Paragraph)MessagePane.Document.Blocks.LastBlock;
            }

            // each message is represented by a Run element
            var run = new Run(message)
            {
                Foreground = messageColor
            };

            // if the paragraph is non-empty, add a line break before the new message
            if (paragraph.Inlines.Count > 0)
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            paragraph.Inlines.Add(run);

            // scroll to the end to show the latest message
            MessagePane.ScrollToEnd();
        }

        public void ClearMessages()
        {
            if (MessagePane.Document != null && MessagePane.Document.Blocks.LastBlock != null)
            {
                ((Paragraph)MessagePane.Document.Blocks.LastBlock).Inlines.Clear();
            }
        }

        public void ShowProgress(string operation, int percentComplete)
        {
            if (percentComplete == ProgressBar.Maximum)
            {
                // the progress complete, reverts back to indeterminate state
                ProgressBar.IsIndeterminate = true;
                StatusText.Text = Title;
            }
            else
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = percentComplete;
                StatusText.Text = operation;
            }
        }
    }
}