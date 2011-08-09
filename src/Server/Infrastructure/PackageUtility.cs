using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Web.Hosting;
using System.Web.Routing;
using NuGet.Server.DataServices;

namespace NuGet.Server.Infrastructure {
    public class PackageUtility {
        private static string _packagePhysicalPath;
        private static string DefaultPackagePhysicalPath = HostingEnvironment.MapPath("~/Packages");
        
        static PackageUtility() {
            // The NuGetPackagePath could be an absolute path (rooted and use as is)
            // or a virtual path (and use as a virtual path)
            string packagePath = ConfigurationManager.AppSettings["NuGetPackagePath"];
            if (String.IsNullOrEmpty(packagePath)) {
                _packagePhysicalPath = DefaultPackagePhysicalPath;
            }
            else {
                if (Path.IsPathRooted(packagePath)) {
                    _packagePhysicalPath = packagePath;
                }
                else {
                    _packagePhysicalPath = HostingEnvironment.MapPath(packagePath);
                }
            }
        }

        public static string PackagePhysicalPath {
            get {
                return _packagePhysicalPath;
            }
        }

        public static Uri GetPackageUrl(Package package, Uri baseUri) {
            Uri packageUri = new Uri(baseUri, GetPackageDownloadUrl(package));
            return packageUri;
        }

        private static string GetPackageDownloadUrl(Package package) {
            var routesValues = new RouteValueDictionary { { "packageId", package.Id }, { "version", package.Version.ToString() } };
            string packageDownloadUrl = RouteTable.Routes["DownloadPackage"].GetVirtualPath(HttpContext.Current.Request.RequestContext, routesValues).VirtualPath;
            return packageDownloadUrl;
        }
    }
}
