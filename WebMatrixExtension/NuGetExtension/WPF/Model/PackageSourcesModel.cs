using System.Collections.Generic;
using System.Linq;

namespace NuGet.WebMatrix
{
    internal class PackageSourcesModel
    {
        private readonly FeedSource _feedSource;
        private IFeedSourceStore _feedSourceStore;

        public PackageSourcesModel(FeedSource feedSource, IFeedSourceStore feedSourceStore)
        {
            _feedSource = feedSource;
            _feedSourceStore = feedSourceStore;
        }

        public IEnumerable<FeedSource> LoadPackageSources()
        {
            List<FeedSource> sources = new List<FeedSource>();
            sources.Add(_feedSource);

            var packageSources = FeedSourceStore.LoadPackageSources();
            sources.AddRange(packageSources);

            return sources;
        }

        internal FeedSource SelectedFeedSource
        {
            get
            {
                return this.FeedSourceStore.SelectedFeed ?? _feedSource;
            }

            set
            {
                this.FeedSourceStore.SelectedFeed = value;
            }
        }

        internal void SavePackageSources(IEnumerable<FeedSource> sources)
        {
            var packageSourcesToSave = sources.Where(s => !s.IsBuiltIn);
            this.FeedSourceStore.SavePackageSources(packageSourcesToSave);
        }

        protected IFeedSourceStore FeedSourceStore
        {
            get
            {
                return _feedSourceStore;
            }
        }
    }
}
