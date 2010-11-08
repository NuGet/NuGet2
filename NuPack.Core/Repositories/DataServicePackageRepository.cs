using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Xml.Linq;

namespace NuGet {
    public class DataServicePackageRepository : PackageRepositoryBase {
        private readonly DataServiceContext _context;
        private readonly IHashProvider _hashProvider;

        public DataServicePackageRepository(Uri serviceRepository)
            : this(serviceRepository, new CryptoHashProvider()) {

        }

        public DataServicePackageRepository(Uri serviceRoot, IHashProvider hashProvider) {
            _context = new DataServiceContext(serviceRoot);
            _hashProvider = hashProvider;

            _context.SendingRequest += OnSendingRequest;
            _context.ReadingEntity += OnReadingEntity;
            _context.IgnoreMissingProperties = true;
        }

        public override IQueryable<IPackage> GetPackages() {
            // REVIEW: Is it ok to assume that the package entity set is called packages?
            return new BatchedDataServiceQuery<DataServicePackage>(_context, "Packages");
        }

        private void OnReadingEntity(object sender, ReadingWritingEntityEventArgs e) {
            var package = (DataServicePackage)e.Entity;
            // REVIEW: This is the only way (I know) to download the package on demand
            package.InitializeDownloader(DownloadAndVerifyPackage(package));
        }

        private void OnSendingRequest(object sender, SendingRequestEventArgs e) {
            // Initialize the request
            HttpWebRequestor.InitializeRequest(e.Request);
        }

        private Func<IPackage> DownloadAndVerifyPackage(DataServicePackage package) {
            Uri downloadUri = _context.GetReadStreamUri(package);
            if (!String.IsNullOrEmpty(package.PackageHash)) {
                byte[] hashBytes = Convert.FromBase64String(package.PackageHash);
                return () => HttpWebRequestor.DownloadPackage(downloadUri, (data) => _hashProvider.VerifyHash(data, hashBytes), useCache: true);
            }
            else {
                return () => HttpWebRequestor.DownloadPackage(downloadUri);
            }
        }
    }
}
