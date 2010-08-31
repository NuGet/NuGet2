namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel.Syndication;
    using System.Xml;

    public class AtomFeedPackageRepository : PackageRepositoryBase {
        private Uri _feedUri;
        
        public AtomFeedPackageRepository(Uri feedUri) {
            if (feedUri == null) {
                throw new ArgumentNullException("feedUri");
            }
            _feedUri = feedUri;
        }

        public override IQueryable<Package> GetPackages() {
            return (from item in GetFeedItems()
                    select new FeedPackage(item)).AsQueryable();
        }

        private IEnumerable<PackageSyndicationItem> GetFeedItems() {
            using (var feedReader = XmlTextReader.Create(_feedUri.OriginalString)) {
                var feed = SyndicationFeed.Load<PackageSyndicationFeed>(feedReader);
                return from PackageSyndicationItem item in feed.Items
                       select item;
            }
        }

        public override void AddPackage(Package package) {
            throw new NotSupportedException();
        }

        public override void RemovePackage(Package package) {
            throw new NotSupportedException();
        }
    }
}
