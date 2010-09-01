using System;
using System.IO;
using System.ServiceModel.Syndication;
using System.Web.Mvc;

namespace NuPack.Server.Controllers {
    public class PackagesController : Controller {
        private const string PackageVirtualPath = "~/Packages";
        //
        // GET: /Packages/

        public ActionResult Index() {
            return View();
        }

        public ActionResult Download(string packageFile) {
            string physicalPath = Server.MapPath(PackageVirtualPath);
            string fullPath = Path.Combine(physicalPath, packageFile);
            return File(fullPath, "application/zip", packageFile);
        }

        public ActionResult Feed() {
            SyndicationFeed packageFeed = PackageSyndicationFeed.Create(
                Server.MapPath(PackageVirtualPath),
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
                                             "application/atom+xml");
        }
        
        private string GetPackageDownloadUrl(Package package) {
            string appRoot = Request.ApplicationPath;
            if (!appRoot.EndsWith("/")) {
                appRoot += "/";
            }

            return String.Format(appRoot + "packages/download?packageFile={0}", GetPackageFileName(package));
        }

        public static string GetPackageFileName(Package package) {
            return package.Id + package.Version + ".nupack";
        }
    }
}
