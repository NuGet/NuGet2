using System;
using System.ServiceModel.Syndication;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuPack.Server.Controllers;
using System.IO;

namespace NuPack.Test.Server.Controllers {
    [TestClass]
    public class SyndicationFeedResultTest {
        [TestMethod]
        public void ExecuteResultSyndicatesFeedWithFormatter() {
            // Arrange
            var items = new SyndicationItem[] { 
                new SyndicationItem("test title", "Some Content", new Uri("http://nupack.com/feed/1"))
            };
            var feed = new SyndicationFeed(items);
            var formatter = new Atom10FeedFormatter(feed);
            Func<SyndicationFeed, SyndicationFeedFormatter> formatterFactory = (f) => formatter;
            var result = new SyndicationFeedResult(feed, formatterFactory);
            var context = new Mock<ControllerContext>();
            var writer = new StringWriter();
            context.Setup(c => c.HttpContext.Response.Output).Returns(writer);

            // Act
            result.ExecuteResult(context.Object);

            // Assert
            string atom = writer.ToString();
            Assert.IsTrue(atom.Contains("test title"));
        }
    }
}
