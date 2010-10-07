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

            DateTime lastModified = System.IO.File.GetLastWriteTimeUtc(fullPath);
            DateTime ifModifiedSince;
            if (DateTime.TryParse(Request.Headers["If-Modified-Since"], out ifModifiedSince) &&
                lastModified > ifModifiedSince) {
                Response.StatusCode = 304;
                return new EmptyResult();
            }

            HttpCachePolicyBase cachePolicy = Response.Cache;
            cachePolicy.SetCacheability(HttpCacheability.Public);
            cachePolicy.SetExpires(DateTime.Now.AddSeconds(30));
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
                package => PackageUtility.GetPackageUrl(package.Id, package.Version.ToString(), Request.Url));



            packageFeed.Title = new TextSyndicationContent("Demo Feed");
            packageFeed.Description = new TextSyndicationContent("Demo package feed");
            SyndicationPerson sp = new SyndicationPerson("person@demofeed.com", "Demo", "http://www.demofeed.com");
            packageFeed.Authors.Add(sp);
            packageFeed.Copyright = new TextSyndicationContent("Copyright " + DateTime.Now.Year);
            packageFeed.Description = new TextSyndicationContent("ASP.NET package feed");
            packageFeed.Language = "en-us";
            packageFeed.LastUpdatedTime = DateTime.Now;

            // Cache the feed for 30 minutes
            ControllerContext.HttpContext.EnableOutputCache(TimeSpan.FromMinutes(30));

            return new SyndicationFeedResult(packageFeed,
                                             feed => new Atom10FeedFormatter(feed),
                                             lastModified,
                                             "application/atom+xml");
        }
    }
}
