using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.WebMatrix.Utility;
using NuGet;

namespace NuGet.WebMatrix.Data
{
    /// <summary>
    /// An IListViewFilter implementation backed by a virtualizing list
    /// </summary>
    internal class VirtualizingListViewFilter : NotifyPropertyChanged, IListViewFilter
    {
        private const int ChunkSize = 50;

        private ListCollectionView _filteredItemsInternal;
        private string _description;
        private string _name;
        private INuGetPackageManager _packageManager;
        private ListCollectionView _unfilteredItems;

        private Expression<Func<IPackage, bool>> _filter;
        private Expression<Func<IPackage, int>> _sort;

        private Task<VirtualizingList> _searchTask;

        public VirtualizingListViewFilter(
            string name, 
            string description,
            Func<object, object> itemFactory)
        {
            this.Name = name;
            this.Description = description;
            this.ItemFactory = itemFactory;
            this.FilteredItemsInternal = new ListCollectionView(new List<object>());
        }

        public bool SupportsPrereleaseFilter
        {
            get
            {
                return true;
            }
        }

        public int Count
        {
            get { return this.UnfilteredItems == null ? 0 : this.UnfilteredItems.Count; }
        }

        public string Description
        {
            get
            {
                return this._description;
            }

            private set
            {
                this._description = value;
                this.OnPropertyChanged("Description");
            }
        }

        public ICollectionView FilteredItems
        {
            get
            {
                return this.FilteredItemsInternal;
            }
        }

        internal ListCollectionView FilteredItemsInternal
        {
            get
            {
                return this._filteredItemsInternal;
            }

            private set
            {
                this._filteredItemsInternal = value;
                this.OnPropertyChanged("FilteredItems");
                this.OnPropertyChanged("FilteredItemsInternal");
            }
        }

        public Func<object, object> ItemFactory
        {
            get;
            private set;
        }

        public string Name
        {
            get
            {
                return this._name;
            }

            private set
            {
                this._name = value;
                this.OnPropertyChanged("Name");
            }
        }

        public override string ToString()
        {
            return Name;
        }

        internal ListCollectionView UnfilteredItems
        {
            get
            {
                return this._unfilteredItems;
            }

            private set
            {
                if (this._unfilteredItems != value)
                {
                    this._unfilteredItems = value;
                    this.OnPropertyChanged("UnfilteredItems");
                    this.OnPropertyChanged("Count");
                }
            }
        }

        internal INuGetPackageManager PackageManager
        {
            get
            {
                return this._packageManager;
            }

            set
            {
                if (this._packageManager != value)
                {
                    this._packageManager = value;
                    this.OnQueryChanged();
                    this.OnPropertyChanged("PackageManager");
                }
            }
        }

        internal Expression<Func<IPackage, bool>> Filter
        {
            get
            {
                return this._filter;
            }

            set
            {
                if (this._filter != value)
                {
                    this._filter = value;
                    this.OnQueryChanged();
                    this.OnPropertyChanged("Filter");
                }
            }
        }

        internal Expression<Func<IPackage, int>> Sort
        {
            get
            {
                return this._sort;
            }

            set
            {
                if (this._sort != value)
                {
                    this._sort = value;
                    this.OnQueryChanged();
                    this.OnPropertyChanged("Sort");
                }
            }
        }

        public void FilterItemsForDisplay(string filterString)
        {
            Interlocked.Exchange(ref _searchTask, null);

            if (String.IsNullOrEmpty(filterString))
            {
                // we cache the original query for when the search is reverted
                this.FilteredItemsInternal = this.UnfilteredItems;
            }
            else
            {
                var list = this.PerformSearch(filterString);
                this.FilteredItemsInternal = new ListCollectionView(list);
            }
        }

        public Task BeginSearch(string searchString, TaskScheduler updateScheduler)
        {
            var searchTask = Task.Factory.StartNew(() => this.PerformSearch(searchString), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            Interlocked.Exchange(ref _searchTask, searchTask);

            return searchTask.ContinueWith(this.CompleteSearch, updateScheduler ?? TaskScheduler.Current);
        }

        private void CompleteSearch(Task<VirtualizingList> task)
        {
            // observe antecedent exception for proper behavior on .NET 4.0
            GC.KeepAlive(task.Exception);

            // only update the results if this is still the newest search task
            if (Interlocked.CompareExchange(ref _searchTask, null, task) == task)
            {
                // faulted searches report 0 results
                this.FilteredItemsInternal = new ListCollectionView(task.IsFaulted ? new object[0] : (IList)task.Result);
            }
        }

        private VirtualizingList PerformSearch(string searchString)
        {
            var query = this.PackageManager.SearchRemotePackages(searchString);

            if (this.Filter != null)
            {
                query = query.Where(this.Filter);
            }

            //// Don't sort the results for search operation by 'Download count' etc
            //// Results returned by the server is based on relevance and is more meaningful in this context

            return new VirtualizingList(query, ChunkSize, ItemFactory = this.ItemFactory);
        }

        private void OnQueryChanged()
        {
            if (this.PackageManager != null)
            {
                Interlocked.Exchange(ref _searchTask, null);
                var query = this.PackageManager.GetRemotePackages();

                if (this.Filter != null)
                {
                    query = query.Where(this.Filter);
                }

                if (this.Sort != null)
                {
                    query = query.OrderByDescending(this.Sort);
                }

                var list = new VirtualizingList(query, ChunkSize, this.ItemFactory);
                this.UnfilteredItems = new ListCollectionView(list);
            }
        }
    }
}
