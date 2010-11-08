using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Xml.Linq;

namespace NuGet {
    public class DataServicePackageRepository : PackageRepositoryBase {
        private readonly DataServiceContext _context;

        public DataServicePackageRepository(Uri serviceRepository)
            : this(serviceRepository, new CryptoHashProvider()) {

        }

        public DataServicePackageRepository(Uri serviceRoot, IHashProvider hashProvider) {
            _context = new DataServiceContext(serviceRoot);
            HashProvider = hashProvider;

            _context.SendingRequest += OnSendingRequest;
            _context.ReadingEntity += OnReadingEntity;
            _context.IgnoreMissingProperties = true;
        }

        public IHashProvider HashProvider { get; set; }

        private void OnReadingEntity(object sender, ReadingWritingEntityEventArgs e) {
            var package = (DataServicePackage)e.Entity;
            package.InitializeDownloader(DownloadAndVerifyPackage(package));
        }

        private Func<IPackage> DownloadAndVerifyPackage(DataServicePackage package) {
            if (!String.IsNullOrEmpty(package.PackageHash)) {
                byte[] hashBytes = Convert.FromBase64String(package.PackageHash);
                return () => HttpWebRequestor.DownloadPackage(_context.GetReadStreamUri(package), (data) => HashProvider.VerifyHash(data, hashBytes), 
                    useCache: true);
            }
            else {
                // REVIEW: This is the only way (I know) to download the package on demand
                // GetReadStreamUri cannot be evaluated inside of OnReadingEntity. Lazily evaluate it inside DownloadPackage
                return () => HttpWebRequestor.DownloadPackage(_context.GetReadStreamUri(package));
            }
        }

        private void OnSendingRequest(object sender, SendingRequestEventArgs e) {
            // Initialize the request
            HttpWebRequestor.InitializeRequest(e.Request);
        }

        public override IQueryable<IPackage> GetPackages() {
            // REVIEW: Is it ok to assume that the package entity set is called packages?
            return new BatchedDataServiceQuery<DataServicePackage>(_context, "Packages");
        }
    }
}
