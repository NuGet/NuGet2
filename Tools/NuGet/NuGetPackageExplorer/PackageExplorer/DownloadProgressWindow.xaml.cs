using System;
using System.Net;
using System.Windows;
using System.Windows.Media;
using NuGet;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for DownloadProgressWindow.xaml
    /// </summary>
    public partial class DownloadProgressWindow : Window {

        private readonly DataServicePackage _package;
        private WebClient _client;

        public DownloadProgressWindow(DataServicePackage package) {
            InitializeComponent();

            _package = package;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            DownloadData(_package.DownloadUrl);
        }

        private void DownloadData(Uri uri) {
            _client = new WebClient();
            _client.DownloadDataCompleted += (sender, e) => {
                if (!e.Cancelled) {
                    if (e.Error != null) {
                        OnError(e.Error);
                    }
                    else {
                        _package.SetData(e.Result);
                        OnCompleted();
                    }
                }
            };

            _client.DownloadProgressChanged += (sender, e) => {
                OnProgress(e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
            };

            _client.DownloadDataAsync(uri);
        }

        private void CancelButtonClicked(object sender, RoutedEventArgs e) {
            Cancel();
        }

        private void Cancel() {
            if (DialogResult == null) {
                DialogResult = false;
            }
        }

        public void OnCompleted() {
            if (DialogResult == null) {
                DialogResult = true;
            }
        }

        public void OnError(Exception error) {
            Progress.Value = 100;
            StatusText.Text = (error.InnerException ?? error).Message;
            StatusText.Foreground = Brushes.Red;
        }

        public void OnProgress(int percentComplete, long bytesReceived, long totalBytes) {
            if (percentComplete < 0) {
                percentComplete = 0;
            }
            if (percentComplete > 100) {
                percentComplete = 100;
            }

            Progress.Value = percentComplete;
            StatusText.Text = String.Format("Downloaded {0}KB of {1}KB...", ToKB(bytesReceived).ToString(), ToKB(totalBytes).ToString());
        }

        private long ToKB(long totalBytes) {
            return (totalBytes + 1023) / 1024;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (_client != null && _client.IsBusy) {
                _client.CancelAsync();
            }
        }
    }
}