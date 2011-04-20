using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using NuGet;
using PackageExplorerViewModel;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for DownloadProgressWindow.xaml
    /// </summary>
    public partial class DownloadProgressWindow : StandardDialog {
        private readonly Uri _downloadUri;
        private WebClient _client;
        private IProxyService _proxyService;

        public DownloadProgressWindow(Uri downloadUri, string id, Version version) {
            if (downloadUri == null) {
                throw new ArgumentNullException("downloadUri");
            }

            InitializeComponent();

            _proxyService = new ProxyService(new AutoDiscoverCredentialProvider());
            Title = "Downloading package " + id + " " + version.ToString();

            _downloadUri = downloadUri;
        }

        public IPackage DownloadedPackage {
            get;
            private set;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Dispatcher.BeginInvoke(new Action<Uri>(DownloadData), DispatcherPriority.Background, _downloadUri);
        }

        private void DownloadData(Uri uri) {
            _client = new WebClient();
            string userAgent = HttpUtility.CreateUserAgentString(PackageExplorerViewModel.Constants.UserAgentClient);
            _client.Headers[HttpRequestHeader.UserAgent] = userAgent;
            // Set the WebClient proxy
            // Maybe we could refactor this code to use the HttpClient so that
            // it can utilize the already existing implementation of getting the proxy
            _client.Proxy = _proxyService.GetProxy(uri);
            _client.DownloadDataCompleted += (sender, e) => {
                if (!e.Cancelled) {
                    if (e.Error != null) {
                        OnError(e.Error);
                    }
                    else {
                        string tempFilePath = SaveResultToTempFile(e.Result);
                        DownloadedPackage = new ZipPackage(tempFilePath);
                        OnCompleted();
                    }
                }
            };

            _client.DownloadProgressChanged += (sender, e) => {
                OnProgress(e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
            };

            _client.DownloadDataAsync(uri);
        }

        private string SaveResultToTempFile(byte[] bytes) {
            string tempFile = Path.GetTempFileName();
            File.WriteAllBytes(tempFile, bytes);
            return tempFile;
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