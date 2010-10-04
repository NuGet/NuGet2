using System;
using System.ServiceModel.Syndication;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace NuPack.Server.Controllers {
    public class SyndicationFeedResult : ActionResult {
        private DateTime _lastModified;
        private string _contentType;
        private SyndicationFeed _feed;
        private Func<SyndicationFeed, SyndicationFeedFormatter> _formatterFactory;

        public SyndicationFeedResult(SyndicationFeed feed,
                                     Func<SyndicationFeed, SyndicationFeedFormatter> formatterFactory,
                                     DateTime lastModified,
                                     string contentType) {
            _feed = feed;
            _formatterFactory = formatterFactory;
            _lastModified = lastModified;
            _contentType = contentType;
        }

        public override void ExecuteResult(ControllerContext context) {
            context.RequestContext.HttpContext.Response.ContentType = _contentType;

            HttpCachePolicyBase cachePolicy = context.RequestContext.HttpContext.Response.Cache;
            cachePolicy.SetCacheability(HttpCacheability.Public);
            // Cache the feed for 30 minutes
            cachePolicy.SetExpires(DateTime.Now.AddMinutes(30));
            cachePolicy.SetValidUntilExpires(true);
            cachePolicy.SetLastModified(_lastModified);

            using (var writer = XmlWriter.Create(context.RequestContext.HttpContext.Response.Output)) {
                var formatter = _formatterFactory(_feed);
                formatter.WriteTo(writer);
            }
        }
    }
}