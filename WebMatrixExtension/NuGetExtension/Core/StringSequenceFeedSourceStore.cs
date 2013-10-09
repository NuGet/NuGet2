using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WebMatrix.Extensibility;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Custom IFeedSourceStore that persists settings to IPreferencesService via IStringSequenceStore.
    /// </summary>
    internal class StringSequenceFeedSourceStore : FeedSourceStore, IFeedSourceStore
    {
        private readonly IStringSequenceStore _stringSequenceStore;

        /// <summary>
        /// The gallery filter tag to use on the feed sources.
        /// </summary>
        private string _filterTag;

        /// <summary>
        /// Initializes a new instance of the StringSequenceFeedSourceStore class.
        /// </summary>
        /// <param name="preferences">IStringSequenceStore instance.</param>
        /// <param name="filterTag">Gallery filter tag for the feed sources.</param>
        public StringSequenceFeedSourceStore(IPreferences preferences, string filterTag)
            : base(preferences)
        {
            _filterTag = filterTag;

            _stringSequenceStore = new StringSequenceStore(this.Preferences);
        }

        /// <summary>
        /// Save the sequence of FeedSources.
        /// </summary>
        /// <param name="sources">Sequence of FeedSources.</param>
        public override void SavePackageSources(IEnumerable<FeedSource> sources)
        {
            _stringSequenceStore.Save(sources.SelectMany(s => new string[] { s.Name, s.SourceUrl.AbsoluteUri }));
        }

        /// <summary>
        /// Loads the sequence of FeedSources.
        /// </summary>
        /// <returns>Sequence of FeedSources.</returns>
        public override IEnumerable<FeedSource> LoadPackageSources()
        {
            var enumerator = _stringSequenceStore.Load().GetEnumerator();
            while (enumerator.MoveNext())
            {
                var name = enumerator.Current;
                if (enumerator.MoveNext())
                {
                    Uri uri;
                    if (Uri.TryCreate(enumerator.Current, UriKind.Absolute, out uri))
                    {
                        yield return new FeedSource(uri, name) { FilterTag = _filterTag };
                    }
                }
            }
        }
    }
}
