using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using NuGet;
using PackageExplorerViewModel;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PackageChooserDialog.xaml
    /// </summary>
    public partial class PackageChooserDialog : Window {
        //private string _currentSort;
        //private bool _ascending;

        public PackageChooserDialog() {
            InitializeComponent();

            DataContext = new PackageChooserViewModel();
        }

        public DataServicePackage SelectedPackage {
            get {
                return PackageGrid.SelectedItem as DataServicePackage;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        private void PackageGrid_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e) {
            string sort = e.Column.SortMemberPath;
            //if (sort == _currentSort) {
            //    _ascending = !_ascending;
            //}
            //else {
            //    _currentSort = sort;
            //    _ascending = true;
            //}

            //e.Column.SortDirection = _ascending ? ListSortDirection.Ascending : ListSortDirection.Descending;

            ICommand sortCommand = (ICommand)PackageGrid.Tag;
            sortCommand.Execute(sort);
            e.Handled = true;
        }

        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                string searchTerm = SearchBox.Text;
                ICommand searchCommand = (ICommand)SearchBox.Tag;
                searchCommand.Execute(searchTerm);
                
                e.Handled = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ICommand loadedCommand = (ICommand)Tag;
            loadedCommand.Execute(null);
        }
    }
}