using System;
using System.Data.Services;
using System.Data.Services.Common;
using System.Data.Services.Providers;
using System.IO;
using System.Web;
using Ninject;
using NuGet.Server.Infrastructure;

namespace NuGet.Server.DataServices {
    // Disabled for live service
    // [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class Packages : DataService<PackageContext>, IDataServiceStreamProvider, IServiceProvider {       
        private IPackageRepository Repository {
            get {
                // It's bad to use the container directly but we aren't in the loop when this 
                // class is created
                return NinjectBootstrapper.Kernel.Get<IPackageRepository>();
            }
        }
        
        // This method is called only once to initialize service-wide policies.
        public static void InitializeService(DataServiceConfiguration config) {
            config.SetEntitySetAccessRule("Packages", EntitySetRights.AllRead);
            // config.SetServiceOperationAccessRule("MyServiceOperation", ServiceOperationRights.All);
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V2;
            config.UseVerboseErrors = true;
        }

        protected override PackageContext CreateDataSource() {
            return new PackageContext(Repository);
        }

        protected override void OnStartProcessingRequest(ProcessRequestArgs args) {
            base.OnStartProcessingRequest(args);

            HttpRequestBase request = new HttpRequestWrapper(HttpContext.Current.Request);
            DateTime lastModified = Directory.GetLastWriteTimeUtc(PackageUtility.PackagePhysicalPath);

            if (request.IsUnmodified(lastModified)) {
                throw new DataServiceException(304, null);
            }

            HttpCachePolicy cachePolicy = HttpContext.Current.Response.Cache;
            cachePolicy.SetCacheability(HttpCacheability.Public);
            cachePolicy.SetLastModified(lastModified);
        }

        public void DeleteStream(object entity, DataServiceOperationContext operationContext) {
            throw new NotSupportedException();
        }

        public Stream GetReadStream(object entity, string etag, bool? checkETagForEquality, DataServiceOperationContext operationContext) {
            throw new NotSupportedException();
        }

        public Uri GetReadStreamUri(object entity, DataServiceOperationContext operationContext) {
            var package = (Package)entity;

            return PackageUtility.GetPackageUrl(package.Id, package.Version, operationContext.AbsoluteServiceUri);
        }

        public string GetStreamContentType(object entity, DataServiceOperationContext operationContext) {
            return "application/zip";
        }

        public string GetStreamETag(object entity, DataServiceOperationContext operationContext) {
            return null;
        }

        public Stream GetWriteStream(object entity, string etag, bool? checkETagForEquality, DataServiceOperationContext operationContext) {
            throw new NotSupportedException();
        }

        public string ResolveType(string entitySetName, DataServiceOperationContext operationContext) {
            throw new NotSupportedException();
        }

        public int StreamBufferSize {
            get {
                return 64000;
            }
        }

        public object GetService(Type serviceType) {
            if (serviceType == typeof(IDataServiceStreamProvider)) {
                return this;
            }
            return null;
        }
    }
}
