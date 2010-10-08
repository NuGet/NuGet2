using System;
using System.Data.Services;
using System.Data.Services.Common;
using System.Data.Services.Providers;
using System.IO;
using System.ServiceModel;
using System.Web;

namespace NuPack.Server.DataServices {
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class Packages : DataService<PackageContext>, IDataServiceStreamProvider, IServiceProvider {
        private bool? _requiresUpdate;        
        
        private bool RequiresUpdate {
            get {
                if (_requiresUpdate == null) {
                    var context = HttpContext.Current;

                    // Enable client caching
                    DateTime lastModified = Directory.GetLastWriteTimeUtc(PackageUtility.PackagePhysicalPath);
                    DateTime ifModifiedSince;
                    if (DateTime.TryParse(context.Request.Headers["If-Modified-Since"], out ifModifiedSince) &&
                        lastModified > ifModifiedSince) {
                        _requiresUpdate = false;
                    }
                    else {
                        _requiresUpdate = true;
                    }
                }
                return _requiresUpdate.Value;
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
            return new PackageContext(new LocalPackageRepository(PackageUtility.PackagePhysicalPath));
        }

        protected override void OnStartProcessingRequest(ProcessRequestArgs args) {
            base.OnStartProcessingRequest(args);

            // HACK: Exceptions for control flow yay! Only way to stop the request from completing
            // right now
            if (!RequiresUpdate) {
                throw new DataServiceException(304, null);
            }

            // Stick the version header in the response
            args.OperationContext.ResponseHeaders[PackageUtility.FeedVersionHeader] = PackageUtility.ODataFeedVersion;

            // Try to determine if this is a request for the feed version
            if(args.OperationContext.RequestHeaders[PackageUtility.FeedVersionHeader] != null) {
                throw new DataServiceException(200, null);
            }

            HttpCachePolicy cachePolicy = HttpContext.Current.Response.Cache;
            cachePolicy.SetCacheability(HttpCacheability.Public);
            cachePolicy.SetExpires(DateTime.Now.AddSeconds(60));
            cachePolicy.SetLastModified(Directory.GetLastWriteTimeUtc(PackageUtility.PackagePhysicalPath));
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
