using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Xml.Linq;

namespace NuGet {
    public class DataServicePackageRepository : PackageRepositoryBase {
        private static readonly XNamespace MetaPropertiesNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
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

            IDictionary<string, string> metaProperties = ReadEntityMetaProperties(e.Data);

            // If the Hash is available, verify it
            string hash;
            if (metaProperties.TryGetValue("PackageHash", out hash)) {
                // REVIEW: This is the only way (I know) to download the package on demand
                package.InitializeDownloader(() =>
                    HttpWebRequestor.DownloadPackage(_context.GetReadStreamUri(package), (data) => HashProvider.VerifyHash(data, Convert.FromBase64String(hash)), true));

            }
            else {
                package.InitializeDownloader(() => HttpWebRequestor.DownloadPackage(_context.GetReadStreamUri(package)));
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

        private static IDictionary<string, string> ReadEntityMetaProperties(XElement data) {
            return (from property in data.Elements(MetaPropertiesNamespace + "properties").Elements()
                    where property.Attribute(MetaPropertiesNamespace + "null") == null
                    select property
                   ).ToDictionary(property => property.Name.LocalName, property => property.Value);
        }
    }
}
