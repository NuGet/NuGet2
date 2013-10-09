using System.ComponentModel;
using System.Windows.Data;

namespace NuGet.WebMatrix.Data
{
    /// <summary>
    /// An interface for filters in the MultiFilterListView
    /// </summary>
    public interface IListViewFilter : INotifyPropertyChanged
    {
        int Count
        {
            get;
        }

        string Description
        {
            get;
        }

        ICollectionView FilteredItems
        {
            get;
        }

        string Name
        {
            get;
        }

        void FilterItemsForDisplay(string filterString);

        bool SupportsPrereleaseFilter
        {
            get;
        }
    }
}
