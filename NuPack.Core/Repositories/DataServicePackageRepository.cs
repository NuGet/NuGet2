namespace NuPack {
    using System;
    using System.Data.Services.Client;
    using System.Linq;
    using System.Net.Cache;

    public class DataServicePackageRepository : PackageRepositoryBase {
        private readonly DataServiceContext _context;
        
        public DataServicePackageRepository(Uri serviceRoot) {
            _context = new DataServiceContext(serviceRoot);

            _context.SendingRequest += OnSendingRequest;
            _context.ReadingEntity += OnReadingEntity;
        }

        private void OnReadingEntity(object sender, ReadingWritingEntityEventArgs e) {
            var package = (DataServicePackage)e.Entity;

            // Set the context so the package can get the download uri later
            package.ServiceContext = _context;
        }
        
        private void OnSendingRequest(object sender, SendingRequestEventArgs e) {
            // Use default credentials, configure the proxy
            e.Request.UseDefaultCredentials = true;
            Utility.ConfigureProxy(e.Request.Proxy);
            // Use the default http cache policy
            e.Request.CachePolicy = new HttpRequestCachePolicy();
        }

        public override IQueryable<IPackage> GetPackages() {
            // REVIEW: Is it ok to assume that the package entity set is called packages?
            return _context.CreateQuery<DataServicePackage>("Packages");
        }
    }
}
