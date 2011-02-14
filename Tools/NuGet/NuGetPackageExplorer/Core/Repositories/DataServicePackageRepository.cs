using System;
using System.Data.Services.Client;
using System.Linq;

namespace NuGet {
    public class DataServicePackageRepository : PackageRepositoryBase {
        private readonly DataServiceContext _context;
        private DataServiceQuery<DataServicePackage> _query;
        private readonly IHttpClient _httpClient;
        private readonly string _source;

        public DataServicePackageRepository(Uri serviceRoot)
            : this(serviceRoot, new HttpClient()) {
        }

        public DataServicePackageRepository(Uri serviceRoot, IHttpClient client)
            : this(new DataServiceContext(serviceRoot), client) {
            _source = serviceRoot.OriginalString;
        }

        private DataServicePackageRepository(DataServiceContext context, IHttpClient httpClient) {
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
        }

        private void OnSendingRequest(object sender, SendingRequestEventArgs e) {
            // Initialize the request
            _httpClient.InitializeRequest(e.Request);
        }

        public override IQueryable<IPackage> GetPackages() {
            if (_query == null) {
                _query = _context.CreateQuery<DataServicePackage>(Constants.PackageServiceEntitySetName);
            }
            return _query;
        }
    }
}
