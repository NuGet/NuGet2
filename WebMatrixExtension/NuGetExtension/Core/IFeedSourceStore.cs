using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.WebMatrix
{
    internal interface IFeedSourceStore
    {
        FeedSource SelectedFeed
        {
            get;
            set;
        }

        void SavePackageSources(IEnumerable<FeedSource> sources);

        IEnumerable<FeedSource> LoadPackageSources();
    }
}
