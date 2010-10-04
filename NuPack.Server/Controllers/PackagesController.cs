using System;
using System.IO;
using System.ServiceModel.Syndication;
using System.Web;
using System.Web.Mvc;

namespace NuPack.Server.Controllers {
    public class PackagesController : Controller {
        private const string PackageVirtualPath = "~/Packages";
        //
        // GET: /Packages/

        public ActionResult Index() {
            return View();
        }

        // ?p=filename
        public ActionResult Download(string p) {
            string physicalPath = Server.MapPath(PackageVirtualPath);
            string fullPath = Path.Combine(physicalPath, p);

            DateTime lastModified = new FileInfo(fullPath).LastAccessTimeUtc;

            HttpCachePolicyBase cachePolicy = Response.Cache;
            cachePolicy.SetCacheability(HttpCacheability.Public);
            cachePolicy.SetExpires(DateTime.Now.AddMinutes(30));
            cachePolicy.SetValidUntilExpires(true);
            // Vary by package file
            cachePolicy.VaryByParams["p"] = true;
            cachePolicy.SetLastModified(lastModified);

            return File(fullPath, "application/zip", p);
        }

        public ActionResult Feed() {
            // Get the full directory path
            string fullPath = Server.MapPath(PackageVirtualPath);

            // Get the last modified of the package directory
            DateTime lastModified = new DirectoryInfo(fullPath).LastWriteTimeUtc;

            SyndicationFeed packageFeed = PackageSyndicationFeed.Create(
                fullPath,
                package => new Uri(Request.Url, GetPackageDownloadUrl(package)));



            packageFeed.Title = new TextSyndicationContent("Demo Feed");
            packageFeed.Description = new TextSyndicationContent("Demo package feed");
            SyndicationPerson sp = new SyndicationPerson("person@demofeed.com", "Demo", "http://www.demofeed.com");
            packageFeed.Authors.Add(sp);
            packageFeed.Copyright = new TextSyndicationContent("Copyright " + DateTime.Now.Year);
            packageFeed.Description = new TextSyndicationContent("ASP.NET package feed");
            packageFeed.Language = "en-us";
            packageFeed.LastUpdatedTime = DateTime.Now;

            return new SyndicationFeedResult(packageFeed,
                                             feed => new Atom10FeedFormatter(feed),
                                             lastModified,
                                             "application/atom+xml");
        }

        private string GetPackageDownloadUrl(IPackage package) {
            string appRoot = Request.ApplicationPath;
            if (!appRoot.EndsWith("/")) {
                appRoot += "/";
            }

            return String.Format(appRoot + "packages/download?p={0}", GetPackageFileName(package));
        }

        public static string GetPackageFileName(IPackage package) {
            return package.Id + "." + package.Version + Constants.PackageExtension;
        }
    }
}
