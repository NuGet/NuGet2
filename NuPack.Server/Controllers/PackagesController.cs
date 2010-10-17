using System;
using System.IO;
using System.ServiceModel.Syndication;
using System.Web.Mvc;
using NuPack.Server.Infrastructure;

namespace NuPack.Server.Controllers {
    public class PackagesController : Controller {
        private readonly IPackageStore _fileSystem;
        private readonly IPackageRepository _repository;

        public PackagesController(IPackageStore fileSystem, IPackageRepository repository) {
            _fileSystem = fileSystem;
            _repository = repository;
        }

        public ActionResult Index() {
            return View();
        }

        // ?p=filename
        public ActionResult Download(string p) {
            DateTimeOffset lastModified = _fileSystem.GetLastModified(p);
            return new ConditionalGetResult(lastModified,
                                            () => File(_fileSystem.GetFullPath(p), "application/zip", p));
        }

        //TODO: This method is deprecated. Need to keep it around till we release NuPack CTP 2.
        [OutputCache(Duration = 60, VaryByParam = "none")]
        public ActionResult Feed() {
            // Add the response header
            Response.AddHeader(PackageUtility.FeedVersionHeader, PackageUtility.AtomFeedVersion);

            if (Request.Headers[PackageUtility.FeedVersionHeader] != null) {
                return new HttpStatusCodeResult(304);
            }

            // Get the last modified of the package directory
            DateTime lastModified = Directory.GetLastWriteTimeUtc(PackageUtility.PackagePhysicalPath);

            return new ConditionalGetResult(lastModified, () => {
                SyndicationFeed packageFeed = PackageSyndicationFeed.Create(
                    _repository,
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
                                                 feed => new Atom10FeedFormatter(feed));
            });
        }
    }
}
