using System;
using System.Data.Services.Client;
using System.Linq;

namespace NuGet {
    public class DataServicePackageRepository : PackageRepositoryBase, IHttpClientEvents {
        private IDataServiceContext _context;
        private readonly IHttpClient _httpClient;
        private readonly PackageDownloader _packageDownloader;

        // Just forward calls to the package downloader
        public event EventHandler<ProgressEventArgs> ProgressAvailable {
            add {
                _packageDownloader.ProgressAvailable += value;
            }
            remove {
                _packageDownloader.ProgressAvailable -= value;
            }
        }

        public event EventHandler<WebRequestEventArgs> SendingRequest {
            add {
                _packageDownloader.SendingRequest += value;
                _httpClient.SendingRequest += value;
            }
            remove {
                _packageDownloader.SendingRequest -= value;
                _httpClient.SendingRequest -= value;
            }
        }

        public DataServicePackageRepository(IHttpClient client){

            if(client == null) {
                throw new ArgumentNullException("client");
            }

            _httpClient = client;
            _httpClient.AcceptCompression = true;

            _packageDownloader = new PackageDownloader(_httpClient);
        }

        public override string Source {
            get {
                return _httpClient.Uri.OriginalString;
            }
        }

        // Don't initialize the Context at the constructor time so that
        // we don't make a web request if we are not gonig to actually use it
        // since gettint the Uri property of th RedirectedHttpClient will
        // trigget that functionality.
        private IDataServiceContext Context {
            get {
                if(_context == null) {
                    _context = new DataServiceContextWrapper(_httpClient.Uri);
                    _context.SendingRequest += OnSendingRequest;
                    _context.ReadingEntity += OnReadingEntity;
                    _context.IgnoreMissingProperties = true;
                }
                return _context;
            }
        }

        private void OnReadingEntity(object sender, ReadingWritingEntityEventArgs e) {
            var package = (DataServicePackage)e.Entity;

            // REVIEW: This is the only way (I know) to download the package on demand
            // GetReadStreamUri cannot be evaluated inside of OnReadingEntity. Lazily evaluate it inside DownloadPackage
            package.Context = Context;
            package.Downloader = _packageDownloader;
        }

        private void OnSendingRequest(object sender, SendingRequestEventArgs e) {
            // Initialize the request
            _httpClient.InitializeRequest(e.Request);
        }

        public override IQueryable<IPackage> GetPackages() {
            // REVIEW: Is it ok to assume that the package entity set is called packages?
            return new SmartDataServiceQuery<DataServicePackage>(Context, Constants.PackageServiceEntitySetName).AsSafeQueryable();
        }
    }
}