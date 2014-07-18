using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NuGet.Tools
{
    /// <summary>
    /// Interaction logic for InfiniteScrollList.xaml
    /// </summary>
    public partial class InfiniteScrollList : UserControl
    {
        private ObservableCollection<object> _items;
        private Loading _loading;
        private ScrollViewer _scrollViewer;

        public event SelectionChangedEventHandler SelectionChanged;

        private CancellationTokenSource _cts;

        public InfiniteScrollList()
        {
            InitializeComponent();
            _loading = new Loading();

            _items = new ObservableCollection<object>();
            _list.ItemsSource = _items;
        }

        public IEnumerable ItemsSource
        {
            get { return _list.ItemsSource; }
        }

        private ILoader _loader;

        public ILoader Loader
        {
            get
            {
                return _loader;
            }
            set
            {
                _loader = value;
                _items.Clear();
                _items.Add(_loading);
                Load();
            }
        }

        private async void Load()
        {
            if (_cts != null)
            {
                // cancel existing async process
                _cts.Cancel();
            }

            var newCts = new CancellationTokenSource();
            _cts = newCts;

            _loading.Status = LoadingStatus.Loading;
            try
            {
                var r = await Loader.LoadItems(_items.Count, _cts.Token);
                _items.RemoveAt(_items.Count - 1);
                foreach (var obj in r.Items)
                {
                    _items.Add(obj);
                }

                if (!r.HasMoreItems)
                {
                    _loading.Status = LoadingStatus.NoMoreItems;
                }
                else
                {
                    _loading.Status = LoadingStatus.Ready;
                }
                _items.Add(_loading);
            }
            catch (OperationCanceledException)
            {
            }

            // When the process is complete, signal that another process can proceed.
            if (_cts == newCts)
            {
                _cts = null;
            }
        }

        public object SelectedItem
        {
            get
            {
                return _list.SelectedItem;
            }
        }

        private void _list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is Loading)
            {
                // make the loading object not selectable
                if (e.RemovedItems.Count > 0)
                {
                    _list.SelectedItem = e.RemovedItems[0];
                }
                else
                {
                    _list.SelectedIndex = -1;
                }
            }
            else
            {
                if (SelectionChanged != null)
                {
                    SelectionChanged(this, e);
                }
            }
        }

        private void _list_Loaded(object sender, RoutedEventArgs e)
        {
            var c = VisualTreeHelper.GetChild(_list, 0);
            _scrollViewer = VisualTreeHelper.GetChild(c, 0) as ScrollViewer;
            _scrollViewer.ScrollChanged += _scrollViewer_ScrollChanged;
        }

        private void _scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_loading.Status == LoadingStatus.Loading ||
                _loading.Status == LoadingStatus.NoMoreItems)
            {
                return;
            }

            var first = _scrollViewer.VerticalOffset;
            var last = _scrollViewer.ViewportHeight + first;
            if (last >= _items.Count)
            {
                Load();
            }
        }
    }

    public class LoadResult
    {
        public IEnumerable Items { get; set; }

        public bool HasMoreItems { get; set; }
    }

    public interface ILoader
    {
        // The second value tells us whether there are more items to load
        Task<LoadResult> LoadItems(int startIndex, CancellationToken ct);
    }

    public enum LoadingStatus
    {
        Ready,
        Loading,
        NoMoreItems
    }

    internal class Loading : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private LoadingStatus _status;

        public LoadingStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }
    }
}