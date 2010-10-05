namespace NuPack.Repositories {
    using System;
    using System.Data.Services.Client;
    using System.Linq;
    using System.Net.Cache;

    public class DataServicePackageRepository : PackageRepositoryBase {
        private readonly DataServiceContext _context;
        private readonly string _packagesEntitySet;

        public DataServicePackageRepository(Uri serviceRoot, string packagesEntitySet) {
            _context = new DataServiceContext(serviceRoot);
            _packagesEntitySet = packagesEntitySet;

            _context.SendingRequest += OnSendingRequest;
        }

        private void OnSendingRequest(object sender, SendingRequestEventArgs e) {
            // Use default credentials, configure the proxy
            e.Request.UseDefaultCredentials = true;
            Utility.ConfigureProxy(e.Request.Proxy);
            // Use the default http cache policy
            e.Request.CachePolicy = new HttpRequestCachePolicy();
        }

        public override IQueryable<IPackage> GetPackages() {
            return _context.CreateQuery<IPackage>(_packagesEntitySet);
        }
    }
}
