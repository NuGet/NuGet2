namespace NuGet {
    using System;
    using System.Data.Services.Client;
    using System.Linq;

    public class DataServicePackageRepository : PackageRepositoryBase {
        private readonly DataServiceContext _context;

        public DataServicePackageRepository(Uri serviceRoot) {
            _context = new DataServiceContext(serviceRoot);

            _context.SendingRequest += OnSendingRequest;
            _context.ReadingEntity += OnReadingEntity;
            _context.IgnoreMissingProperties = true;
        }

        private void OnReadingEntity(object sender, ReadingWritingEntityEventArgs e) {
            var package = (DataServicePackage)e.Entity;

            // REVIEW: This is the only way (I know) to download the package on demand
            package.InitializeDownloader(() => HttpWebRequestor.DownloadPackage(_context.GetReadStreamUri(package)));
        }

        private void OnSendingRequest(object sender, SendingRequestEventArgs e) {
            // Initialize the request
            HttpWebRequestor.InitializeRequest(e.Request);
        }

        public override IQueryable<IPackage> GetPackages() {
            // REVIEW: Is it ok to assume that the package entity set is called packages?
            return _context.CreateQuery<DataServicePackage>("Packages");
        }
    }
}
