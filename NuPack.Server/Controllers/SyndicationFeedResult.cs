using System;
using System.ServiceModel.Syndication;
using System.Web.Mvc;
using System.Xml;

namespace NuPack.Server.Controllers {
    public class SyndicationFeedResult : ActionResult {
        private SyndicationFeed _feed;
        private Func<SyndicationFeed, SyndicationFeedFormatter> _formatterFactory;

        public SyndicationFeedResult(SyndicationFeed feed,
                                     Func<SyndicationFeed, SyndicationFeedFormatter> formatterFactory) {
            _feed = feed;
            _formatterFactory = formatterFactory;
        }

        public override void ExecuteResult(ControllerContext context) {
            using (var writer = XmlWriter.Create(context.HttpContext.Response.Output)) {
                var formatter = _formatterFactory(_feed);
                formatter.WriteTo(writer);
            }
        }
    }
}