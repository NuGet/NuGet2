using System;
using System.Data.Services.Client;
using System.Linq;

namespace NuGet {
    public class DataServicePackageRepository : PackageRepositoryBase, IHttpClientEvents {
        private readonly IDataServiceContext _context;
        private readonly IHttpClient _httpClient;
        private readonly string _source;
        private readonly PackageDownloader _packageDownloader = new PackageDownloader();

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

        public DataServicePackageRepository(Uri serviceRoot)
            : this(serviceRoot, new HttpClient()) {
        }

        public DataServicePackageRepository(Uri serviceRoot, IHttpClient client)
            : this(new DataServiceContextWrapper(serviceRoot), client) {
            _source = serviceRoot.OriginalString;
        }

        private DataServicePackageRepository(IDataServiceContext context, IHttpClient httpClient) {
            if (context == null) {
                throw new ArgumentNullException("context");
            }

            if (httpClient == null) {
                throw new ArgumentNullException("httpClient");
            }

            _context = context;
            _httpClient = httpClient;

            _context.SendingRequest += OnSendingRequest;
            _context.ReadingEntity += OnReadingEntity;
            _context.IgnoreMissingProperties = true;
        }

        public override string Source {
            get {
                return _source;
            }
        }

        private void OnReadingEntity(object sender, ReadingWritingEntityEventArgs e) {
            var package = (DataServicePackage)e.Entity;

            // REVIEW: This is the only way (I know) to download the package on demand
            // GetReadStreamUri cannot be evaluated inside of OnReadingEntity. Lazily evaluate it inside DownloadPackage
            package.Context = _context;
            package.Downloader = _packageDownloader;
        }

        private void OnSendingRequest(object sender, SendingRequestEventArgs e) {
            // Initialize the request
            _httpClient.InitializeRequest(e.Request, acceptCompression: true);
        }

        public override IQueryable<IPackage> GetPackages() {
            // REVIEW: Is it ok to assume that the package entity set is called packages?
            return new SmartDataServiceQuery<DataServicePackage>(_context, Constants.PackageServiceEntitySetName).AsSafeQueryable();
        }
    }
}