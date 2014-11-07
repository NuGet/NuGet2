using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Resx = NuGet.Client.VisualStudio.UI.Resources;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// Interaction logic for InfiniteScrollList.xaml
    /// </summary>
    public partial class InfiniteScrollList : UserControl
    {
        private ObservableCollection<object> _items;
        private LoadingStatusIndicator _loadingStatusIndicator;
        private ScrollViewer _scrollViewer;
        
        public event SelectionChangedEventHandler SelectionChanged;

        private CancellationTokenSource _cts;

        public InfiniteScrollList()
        {
            InitializeComponent();
            _loadingStatusIndicator = new LoadingStatusIndicator();

            _items = new ObservableCollection<object>();
            _list.ItemsSource = _items;
        }

        public ObservableCollection<object> Items
        {
            get
            {
                return _items;
            }
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
                Reload();
            }
        }

        // Reload items starting with index 0
        public void Reload()
        {
            _items.Clear();
            _items.Add(_loadingStatusIndicator);
            Load();
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

            _loadingStatusIndicator.Status = LoadingStatus.Loading;
            try
            {
                // Note that the last item is _loadingStatusIndicator. So the start index of the
                // items to load is _items.Count - 1.
                var r = await Loader.LoadItems(_items.Count - 1, _cts.Token);

                _items.RemoveAt(_items.Count - 1);
                foreach (var obj in r.Items)
                {
                    _items.Add(obj);
                }

                if (!r.HasMoreItems)
                {
                    if (_items.Count == 0)
                    {
                        _loadingStatusIndicator.Status = LoadingStatus.NoItemsFound;
                    }
                    else
                    {
                        _loadingStatusIndicator.Status = LoadingStatus.NoMoreItems;
                    }
                }
                else
                {
                    _loadingStatusIndicator.Status = LoadingStatus.Ready;
                }
                _items.Add(_loadingStatusIndicator);

                // select the first item if none was selected before
                if (_list.SelectedIndex == -1 && _items.Count > 1)
                {
                    _list.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException ||
                    (ex is AggregateException &&
                    ex.GetBaseException() is OperationCanceledException))
                {
                    // do nothing
                }
                else
                {
                    var message = String.Format(
                        CultureInfo.CurrentCulture,
                        Resx.Resources.Text_ErrorOccurred,
                        ex);
                    _loadingStatusIndicator.Status = LoadingStatus.ErrorOccured;
                    _loadingStatusIndicator.ErrorMessage = message;
                }
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
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is LoadingStatusIndicator)
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
            var c = VisualTreeHelper.GetChild(_list, 0) as Border;
            if (c == null)
            {
                return;
            }

            c.Padding = new Thickness(0);
            _scrollViewer = VisualTreeHelper.GetChild(c, 0) as ScrollViewer;
            if (_scrollViewer == null)
            {
                return;
            }

            _scrollViewer.Padding = new Thickness(0);
            _scrollViewer.ScrollChanged += _scrollViewer_ScrollChanged;
        }

        private void _scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_loadingStatusIndicator.Status != LoadingStatus.Ready)
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

        private void RetryButtonClicked(object sender, RoutedEventArgs e)
        {
            Load();
        }

        internal void SelectFirstItem()
        {
            if (_list.Items.Count > 0)
            {
                _list.SelectedIndex = 0;
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
        NoMoreItems,
        NoItemsFound,
        ErrorOccured
    }

    internal class LoadingStatusIndicator : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private LoadingStatus _status;

        private string _errorMessage;

        public LoadingStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }
        
        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged("ErrorMessage");
                }
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