using System;
using System.IO;
using System.ServiceModel.Syndication;
using System.Web;
using System.Web.Mvc;

namespace NuPack.Server.Controllers {
    public class PackagesController : Controller {
        public ActionResult Index() {
            return View();
        }

        // ?p=filename
        public ActionResult Download(string p) {
            string fullPath = Path.Combine(PackageUtility.PackagePhysicalPath, p);

            DateTime lastModified = System.IO.File.GetLastWriteTimeUtc(fullPath);
            return ConditionalGet(lastModified, () => File(fullPath, "application/zip", p));
        }

        public ActionResult Feed() {            
            // Add the response header
            Response.AddHeader(PackageUtility.FeedVersionHeader, PackageUtility.AtomFeedVersion);

            if (Request.Headers[PackageUtility.FeedVersionHeader] != null) {
                Response.StatusCode = 304;
                return new EmptyResult();
            }

            // Get the last modified of the package directory
            DateTime lastModified = Directory.GetLastWriteTimeUtc(PackageUtility.PackagePhysicalPath);

            return ConditionalGet(lastModified, () => {
                SyndicationFeed packageFeed = PackageSyndicationFeed.Create(
                    PackageUtility.PackagePhysicalPath,
                    package => PackageUtility.GetPackageUrl(package.Id, package.Version.ToString(), Request.Url));


                packageFeed.Title = new TextSyndicationContent("Demo Feed");
                packageFeed.Description = new TextSyndicationContent("Demo package feed");
                SyndicationPerson sp = new SyndicationPerson("person@demofeed.com", "Demo", "http://www.demofeed.com");
                packageFeed.Authors.Add(sp);
                packageFeed.Copyright = new TextSyndicationContent("Copyright " + DateTime.Now.Year);
                packageFeed.Description = new TextSyndicationContent("ASP.NET package feed");
                packageFeed.Language = "en-us";
                packageFeed.LastUpdatedTime = lastModified;

                return new SyndicationFeedResult(packageFeed,
                                                 feed => new Atom10FeedFormatter(feed),
                                                 lastModified,
                                                 "application/atom+xml");
            }, cacheDuration: 60);
        }

        private ActionResult ConditionalGet(DateTime lastModified,
                                            Func<ActionResult> action,
                                            int cacheDuration = 30) {
            DateTime ifModifiedSince;
            if (DateTime.TryParse(Request.Headers["If-Modified-Since"], out ifModifiedSince) &&
                lastModified > ifModifiedSince) {
                Response.StatusCode = 304;
                return new EmptyResult();
            }

            HttpCachePolicyBase cachePolicy = Response.Cache;
            cachePolicy.SetCacheability(HttpCacheability.Public);
            cachePolicy.SetExpires(DateTime.Now.AddSeconds(cacheDuration));
            cachePolicy.SetLastModified(lastModified);

            return action();
        }
    }
}
