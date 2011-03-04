using System;
using System.Windows.Input;
using NuGet;
using System.IO;

namespace PackageExplorerViewModel {

    public class PublishPackageViewModel : ViewModelBase, IObserver<int> {
        private IPackageMetadata _package;
        private Lazy<Stream> _packageStream;

        public PublishPackageViewModel(IPackageMetadata package, Func<Stream> getPackageStream) {
            if (package == null) {
                throw new ArgumentNullException("package");
            }

            if (getPackageStream == null) {
                throw new ArgumentNullException("getPackageStream");
            }
            _package = package;
            _packageStream = new Lazy<Stream>(getPackageStream);
        }

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

        private bool _canPublish = true;

        public bool CanPublish
        {
            get { return _canPublish; }
            set {
                if (_canPublish != value)
                {
                    _canPublish = value;
                    RaisePropertyChangeEvent("CanPublish");
                }
            }
        }

        private GalleryServer _uploadHelper;

        public GalleryServer GalleryServer {
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

        public void PushPackage() {
            PercentComplete = 0;
            ShowProgress = true;
            Status = "Uploading package...";
            HasError = false;
            CanPublish = false;

            Stream fileStream = _packageStream.Value;
            fileStream.Seek(0, SeekOrigin.Begin);

            GalleryServer.CreatePackage(PublishKey, fileStream, this, PushOnly == true? (IPackageMetadata)null : _package);
        }

        public void OnCompleted() {
            PercentComplete = 100;
            HasError = false;
            Status = "Package published successfully .";
        }

        public void OnError(Exception error) {
            PercentComplete = 100;
            ShowProgress = false;
            HasError = true;
            Status = error.Message;
            CanPublish = true;
        }

        public void OnNext(int value) {
            PercentComplete = value;
        }
    }
}