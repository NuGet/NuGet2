using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace NuGet.WebMatrix.Tests.Utilities
{
    internal class InMemoryFeedSourceStore : IFeedSourceStore
    {
        public InMemoryFeedSourceStore(FeedSource source, IEnumerable<FeedSource> sources = null)
        {
            Assert.NotNull(source);

            this.Sources = new List<FeedSource>(sources ?? new FeedSource[] { source });
            this.SelectedFeed = source;

            Assert.Contains(this.SelectedFeed, this.Sources);
        }

        public List<FeedSource> Sources
        {
            get;
            private set;
        }

        public IEnumerable<FeedSource> LoadPackageSources()
        {
            return this.Sources;
        }

        public void SavePackageSources(IEnumerable<FeedSource> sources)
        {
            this.Sources = sources.ToList();
        }

        public FeedSource SelectedFeed
        {
            get;
            set;
        }
    }
}
