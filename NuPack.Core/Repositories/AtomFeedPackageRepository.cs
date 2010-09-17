namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
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

        public override IQueryable<Package> GetPackages() {
            return (from item in GetFeedItems()
                    select new FeedPackage(item)).AsQueryable();
        }

        internal IEnumerable<PackageSyndicationItem> GetFeedItems() {
            try {
                var request = WebRequest.Create(_feedUri);
                using (var response = request.GetResponse()) {
                    using (var stream = response.GetResponseStream()) {
                        using (var feedReader = XmlTextReader.Create(stream)) {
                            return GetFeedItems(feedReader);
                        }
                    }
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


        public override void AddPackage(Package package) {
            throw new NotSupportedException();
        }

        public override void RemovePackage(Package package) {
            throw new NotSupportedException();
        }
    }
}
