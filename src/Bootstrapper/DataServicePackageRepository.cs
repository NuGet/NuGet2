using System;
using System.Data.Services.Client;
using System.Linq;

namespace NuGet {
    public class DataServicePackageRepository {
        internal const string PackageServiceEntitySetName = "Packages";

        private DataServiceContext _context;
        private readonly IHttpClient _httpClient;

        public DataServicePackageRepository(IHttpClient client) {
            if (client == null) {
                throw new ArgumentNullException("client");
            }

            _httpClient = client;
            _httpClient.AcceptCompression = true;
        }

        // Don't initialize the Context at the constructor time so that
        // we don't make a web request if we are not going to actually use it
        // since getting the Uri property of the RedirectedHttpClient will
        // trigger that functionality.
        private DataServiceContext Context {
            get {
                if (_context == null) {
                    _context = new DataServiceContext(_httpClient.Uri);
                    _context.SendingRequest += OnSendingRequest;
                    _context.IgnoreMissingProperties = true;
                }
                return _context;
            }
        }

        private void OnSendingRequest(object sender, SendingRequestEventArgs e) {
            _httpClient.InitializeRequest(e.Request);
        }

        public Uri GetReadStreamUri(object entity) {
            return Context.GetReadStreamUri(entity);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IQueryable<DataServicePackage> GetPackages() {
            return Context.CreateQuery<DataServicePackage>(PackageServiceEntitySetName);
        }
    }
}