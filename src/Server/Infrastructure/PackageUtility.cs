using System;
using System.Web;
using System.Web.Hosting;
using System.Configuration;

namespace NuGet.Server.Infrastructure
{
    public class PackageUtility
    {

        internal static string PackagePhysicalPath;
        private static string DefaultPackagePhysicalPath = HostingEnvironment.MapPath("~/Packages");


        static PackageUtility()
        {
            string packagePath = ConfigurationManager.AppSettings["NuGetPackagePath"];
            if (string.IsNullOrEmpty(packagePath))
            {
                PackagePhysicalPath = DefaultPackagePhysicalPath;
            }
            else
            {
                PackagePhysicalPath = packagePath;
            }
        }


        public static Uri GetPackageUrl(string path, Uri baseUri)
        {
            return new Uri(baseUri, GetPackageDownloadUrl(path));
        }

        private static string GetPackageDownloadUrl(string path)
        {
            return VirtualPathUtility.ToAbsolute("~/Packages/" + path);
        }
    }
}
