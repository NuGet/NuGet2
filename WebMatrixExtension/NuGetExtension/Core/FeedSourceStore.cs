using System.Collections.Generic;
using System.Linq;
using Microsoft.WebMatrix.Extensibility;

namespace NuGet.WebMatrix
{
    internal abstract class FeedSourceStore : IFeedSourceStore
    {
        private string SelectedFeedPreferencesKey = "SelectedFeed";

        private FeedSource _selectedFeed;

        protected FeedSourceStore(IPreferences preferences)
        {
            this.Preferences = preferences;
        }

        public IPreferences Preferences
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the selected feed, which might be null
        /// </summary>
        public FeedSource SelectedFeed
        {
            get
            {
                if (_selectedFeed == null)
                {
                    // try to match the stored name exactly
                    var name = this.Preferences.GetValue(SelectedFeedPreferencesKey);
                    _selectedFeed = this.LoadPackageSources().FirstOrDefault(f => f.Name.Equals(name));
                }

                return _selectedFeed;
            }

            set
            {
                if (value != null)
                {
                    this.Preferences.SetValue(SelectedFeedPreferencesKey, value.Name);
                    _selectedFeed = value;
                }
            }
        }

        public abstract void SavePackageSources(IEnumerable<FeedSource> sources);

        public abstract IEnumerable<FeedSource> LoadPackageSources();
    }
}
