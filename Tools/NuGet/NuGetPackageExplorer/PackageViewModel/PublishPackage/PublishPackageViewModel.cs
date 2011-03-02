using System;
using System.Windows.Input;
using NuGet;
using System.IO;

namespace PackageExplorerViewModel {

    public class PublishPackageViewModel : ViewModelBase, IObserver<int> {
        private IPackageMetadata _package;
        private Stream _packageStream;

        public PublishPackageViewModel(IPackageMetadata package, Stream packageStream) {
            if (package == null) {
                throw new ArgumentNullException("package");
            }

            if (packageStream == null) {
                throw new ArgumentNullException("packageStream");
            }
            _package = package;
            _packageStream = packageStream;
            PublishCommand = new PublishCommand(this);
        }

        public ICommand PublishCommand { get; private set; }

        private string _publishKey; 

        public string PublishKey {
            get { return _publishKey; }
            set {
                if (_publishKey != value) {
                    _publishKey = value;
                    RaisePropertyChangeEvent("PublishKey");
                }
            }
        }

        private bool? _pushOnly = false;

        public bool? PushOnly {
            get {
                return _pushOnly;
            }
            set {
                if (_pushOnly != value) {
                    _pushOnly = value;
                    RaisePropertyChangeEvent("PushOnly");
                }
            }
        }

        public string Id {
            get { return _package.Id; }
        }

        public string Version {
            get { return _package.Version.ToString(); }
        }

        private bool _hasError;

        public bool HasError {
            get {
                return _hasError;
            }
            set {
                if (_hasError != value) {
                    _hasError = value;
                    RaisePropertyChangeEvent("HasError");
                }
            }
        }

        private bool _showProgress;
        public bool ShowProgress {
            get { return _showProgress; }
            set {
                if (_showProgress != value) {
                    _showProgress = value;
                    RaisePropertyChangeEvent("ShowProgress");
                }
            }
        }

        private int _percentComplete;
        public int PercentComplete {
            get { return _percentComplete; }
            set {
                if (_percentComplete != value) {
                    _percentComplete = value;
                    RaisePropertyChangeEvent("PercentComplete");
                }
            }
        }

        private GalleryServer _uploadHelper;

        public GalleryServer UploadHelper {
            get {
                if (_uploadHelper == null) {
                    _uploadHelper = new GalleryServer();
                }
                return _uploadHelper;
            }
        }

        private string _status;

        public string Status {
            get { return _status; }
            set {
                if (_status != value) {
                    _status = value;
                    RaisePropertyChangeEvent("Status");
                }
            }
        }

        internal void PushPackage() {
            PercentComplete = 0;
            ShowProgress = true;
            Status = "Uploading package...";
            HasError = false;

            UploadHelper.CreatePackage(PublishKey, _packageStream, this, PushOnly == true? (IPackageMetadata)null : _package);
        }

        public void OnCompleted() {
            PercentComplete = 100;
            HasError = false;
        }

        public void OnError(Exception error) {
            Status = error.Message;
            PercentComplete = 100;
            HasError = true;
        }

        public void OnNext(int value) {
            PercentComplete = value;
        }
    }
}