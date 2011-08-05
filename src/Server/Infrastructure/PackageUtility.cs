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
            string packagePath = ConfigurationManager.AppSettings["NuGetPackagePath"];
            if (string.IsNullOrEmpty(packagePath)) {
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
            return new Uri(baseUri, GetPackageDownloadUrl(package));
        }

        private static string GetPackageDownloadUrl(Package package) {
            return RouteTable.Routes["DownloadPackage"].GetVirtualPath(HttpContext.Current.Request.RequestContext, new RouteValueDictionary { { "packageId", package.Id }, { "version", package.Version.ToString().Replace('.', '_') } }).VirtualPath;
        }
    }
}
