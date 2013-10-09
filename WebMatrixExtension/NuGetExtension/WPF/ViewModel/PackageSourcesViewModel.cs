using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.WebMatrix.Utility;

namespace NuGet.WebMatrix
{
    internal class PackageSourcesViewModel : NotifyPropertyChanged
    {
        private PackageSourcesModel _packageSourcesModel;
        private ObservableCollection<FeedSource> _feedSources;
        private string _newSourceName;
        private string _newSourceUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:PackageSourcesViewModel"/> class.
        /// </summary>
        public PackageSourcesViewModel(PackageSourcesModel packageSourcesModel)
        {
            // Setup the package sources
            _packageSourcesModel = packageSourcesModel;

            InitializeCommands();
            InitializeFeedSources();
        }

        private void InitializeCommands()
        {
            AddCommand = new RelayCommand(
                this.AddNewSource,
                this.CanAddNewSource);

            DeleteCommand = new RelayCommand(
                (state) => DeleteFeedSource(state as FeedSource),
                (state) =>
                {
                    return state is FeedSource;
                });

            SaveCommand = new RelayCommand(Save);

            CancelCommand = new RelayCommand(Cancel);
        }

        private void DeleteFeedSource(FeedSource feedSource)
        {
            if (this.FeedSources.Contains(feedSource))
            {
                this.FeedSources.Remove(feedSource);
            }
        }

        private void Save(object unused)
        {
            _packageSourcesModel.SavePackageSources(FeedSources);

            RefreshFeedSources();
        }

        private void Cancel(object unused)
        {
            RefreshFeedSources();
        }

        private void InitializeFeedSources()
        {
            RefreshFeedSources();
        }

        public ICommand AddCommand
        {
            get;
            private set;
        }

        public ICommand DeleteCommand
        {
            get;
            private set;
        }

        public ICommand SaveCommand
        {
            get;
            private set;
        }

        public RelayCommand CancelCommand
        {
            get;
            private set;
        }

        public string NewSourceName
        {
            get
            {
                return _newSourceName;
            }

            set
            {
                _newSourceName = value;
                OnPropertyChanged("NewSourceName");
            }
        }

        public string NewSourceUrl
        {
            get
            {
                return _newSourceUrl;
            }

            set
            {
                _newSourceUrl = value;
                OnPropertyChanged("NewSourceUrl");
            }
        }

        public FeedSource ActiveFeedSource
        {
            get
            {
                return this._packageSourcesModel.SelectedFeedSource;
            }

            set
            {
                this._packageSourcesModel.SelectedFeedSource = value;
                OnPropertyChanged("ActiveFeedSource");
            }
        }

        public ObservableCollection<FeedSource> FeedSources
        {
            get
            {
                return _feedSources;
            }

            private set
            {
                _feedSources = value;
                OnPropertyChanged("FeedSources");
            }
        }

        private void RefreshFeedSources()
        {
            FeedSources = new ObservableCollection<FeedSource>(_packageSourcesModel.LoadPackageSources());

            if (!FeedSources.Contains(ActiveFeedSource))
            {
                ActiveFeedSource = FeedSources[0];
            }

            OnPropertyChanged("ActiveFeedSource");
        }

        private void AddNewSource(object ignore)
        {
            FeedSource newSource = new FeedSource(new Uri(this.NewSourceUrl), this.NewSourceName);
            this.FeedSources.Add(newSource);

            this.NewSourceName = string.Empty;
            this.NewSourceUrl = string.Empty;
        }

        private bool CanAddNewSource(object ignore)
        {
            if (String.IsNullOrWhiteSpace(this.NewSourceName) || String.IsNullOrWhiteSpace(this.NewSourceUrl))
            {
                return false;
            }

            // TryCreate handles a number of special cases where the string is not in a canonical URI format,
            // this include plain-old-file-paths 'C:\some\file.txt'
            // and also file URIs with backslashes 'file://C:\some\file.txt'
            Uri uri;
            if (!Uri.TryCreate(this.NewSourceUrl, UriKind.Absolute, out uri))
            {
                return false;
            }

            // name must be unique
            return !FeedSources.Any((feedSource) => this.NewSourceName.EqualsIgnoreCase(feedSource.Name));
        }
    }
}
