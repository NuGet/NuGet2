using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using Microsoft.WebMatrix.Utility;

namespace NuGet.WebMatrix.Data
{
    public class ListViewFilter : NotifyPropertyChanged, IListViewFilter
    {
        /// <summary>
        /// Initializes a new instance of the ListViewFilter class
        /// </summary>
        /// <param name="name">The name of the filter</param>
        /// <param name="description">The description of the filter</param>
        public ListViewFilter(string name, string description, bool supportsPrerelease)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "name is null or empty");

            this.Name = name;
            this.Description = description;
            this.SupportsPrereleaseFilter = supportsPrerelease;

            this.Items = new ObservableCollection<ListViewItemWrapper>();

            FilteredItems = CollectionViewSource.GetDefaultView(Items);
            FilteredItems.Filter = FilterPredicate;
            FilteredItems.SortDescriptions.Add(new SortDescription("Priority", ListSortDirection.Ascending));
            FilteredItems.SortDescriptions.Add(new SortDescription("CompareString", ListSortDirection.Ascending));

            this.Items.CollectionChanged += new NotifyCollectionChangedEventHandler(OnItemsCollectionChanged);
        }

        private bool FilterPredicate(object item)
        {
            bool result = true;
            var listViewItem = item as ListViewItemWrapper;

            if (listViewItem != null && !listViewItem.Visible)
            {
                result = false;
            }

            return result;
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // The count of items might have changed
            OnPropertyChanged("Count");
        }

        public int Count
        {
            get
            {
                return this.Items.Count;
            }
        }

        public string Name
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        public bool SupportsPrereleaseFilter
        {
            get;
            private set;
        }

        public ICollectionView FilteredItems
        {
            get;
            private set;
        }

        public ObservableCollection<ListViewItemWrapper> Items
        {
            get;
            private set;
        }

        protected virtual void OnFilterItemsForDisplay(string filterString)
        {
            bool shouldRefresh = false;

            if (string.IsNullOrEmpty(filterString))
            {
                foreach (ListViewItemWrapper wrapper in Items)
                {
                    if (!wrapper.Visible)
                    {
                        wrapper.Visible = true;
                        shouldRefresh = true;
                    }
                }
            }
            else
            {
                foreach (ListViewItemWrapper wrapper in Items)
                {
                    string sourceText = (!string.IsNullOrEmpty(wrapper.SearchText)) ? wrapper.SearchText : wrapper.Name;
                    if (Contains(sourceText, filterString, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (!wrapper.Visible)
                        {
                            wrapper.Visible = true;
                            shouldRefresh = true;
                        }
                    }
                    else
                    {
                        if (wrapper.Visible)
                        {
                            wrapper.Visible = false;
                            shouldRefresh = true;
                        }
                    }
                }
            }

            if (shouldRefresh)
            {
                FilteredItems.Refresh();
            }
        }

        public void FilterItemsForDisplay(string filterString)
        {
            this.OnFilterItemsForDisplay(filterString);
        }

        private static bool Contains(string source, string toCheck, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            // split on the words and check that each is present
            bool contains = true;
            foreach (string word in toCheck.Split(' '))
            {
                if (!(source.IndexOf(word, comparison) >= 0))
                {
                    contains = false;
                    break;
                }
            }

            return contains;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ListViewItemWrapper : NotifyPropertyChanged, IComparable<ListViewItemWrapper>
    {
        public ListViewItemWrapper()
        {
            this.IsEnabled = true;
        }

        public virtual string Name
        {
            get;
            set;
        }

        public object Item
        {
            get;
            set;
        }

        public bool Visible
        {
            get;
            set;
        }

        public double Priority
        {
            get;
            set;
        }

        public string SearchText
        {
            get;
            set;
        }

        public string CompareString
        {
            get;
            set;
        }

        public bool IsEnabled
        {
            get;
            set;
        }

        public string ToolTip
        {
            get;
            set;
        }

        public int CompareTo(ListViewItemWrapper other)
        {
            int retVal;
            if (other == null)
            {
                Debug.Fail("why compare with null");
                retVal = 1;
            }
            else if (object.ReferenceEquals(this, other))
            {
                retVal = 0;
            }
            else
            {
                retVal = this.Priority.CompareTo(other.Priority);
                if (retVal == 0)
                {
                    retVal = string.CompareOrdinal(this.CompareString, other.CompareString);
                }
            }

            return retVal;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
