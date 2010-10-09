namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Cache;
    using System.ServiceModel.Syndication;
    using System.Xml;
    using NuPack.Resources;

    public class AtomFeedPackageRepository : PackageRepositoryBase {
        private Uri _feedUri;

        public AtomFeedPackageRepository(Uri feedUri) {
            if (feedUri == null) {
                throw new ArgumentNullException("feedUri");
            }
            _feedUri = feedUri;
        }

        public override IQueryable<IPackage> GetPackages() {
            return (from item in GetFeedItems()
                    select new FeedPackage(item)).AsQueryable();
        }

        internal IEnumerable<PackageSyndicationItem> GetFeedItems() {
            return GetFeedItems(() => HttpWebRequestor.GetResponseStream(_feedUri));
        }

        internal static IEnumerable<PackageSyndicationItem> GetFeedItems(Func<Stream> getStream) {
            try {
                using (var feedReader = XmlTextReader.Create(getStream())) {
                    return GetFeedItems(feedReader);
                }
            }
            catch (WebException exception) {
                throw new InvalidOperationException(NuPackResources.AtomFeedPackageRepo_InvalidFeedSource, exception);
            }
        }

        internal static IEnumerable<PackageSyndicationItem> GetFeedItems(XmlReader xmlReader) {
            try {
                var feed = SyndicationFeed.Load<PackageSyndicationFeed>(xmlReader);
                return from PackageSyndicationItem item in feed.Items
                       select item;
            }
            catch (XmlException exception) {
                throw new InvalidOperationException(NuPackResources.AtomFeedPackageRepo_InvalidFeedContent, exception);
            }
        }
    }
}
