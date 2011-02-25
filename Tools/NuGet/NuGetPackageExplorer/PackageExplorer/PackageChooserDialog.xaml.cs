using System;
using System.Windows;
using System.Windows.Input;
using NuGet;
using PackageExplorerViewModel;
using System.ComponentModel;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PackageChooserDialog.xaml
    /// </summary>
    public partial class PackageChooserDialog : DialogWithNoMinimizeAndMaximize {

        public PackageChooserDialog() {
            InitializeComponent();

            var viewModel = new PackageChooserViewModel();
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            DataContext = viewModel;
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            string name = e.PropertyName;
            if (name.Equals("CurrentSortColumn") || name.Equals("SortDirection")) {
                var viewModel = (PackageChooserViewModel)sender;
                string columnName = viewModel.CurrentSortColumn;
                ListSortDirection direction = (ListSortDirection)viewModel.SortDirection;

                foreach (var column in PackageGrid.Columns) {
                    if (column.SortMemberPath == columnName) {
                        column.SortDirection = direction;
                        break;
                    }
                }
            }
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
            ICommand sortCommand = (ICommand)PackageGrid.Tag;
            sortCommand.Execute(e.Column.SortMemberPath);
            e.Handled = true;
        }

        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            string searchTerm = null;

            if (e.Key == Key.Enter) {
                searchTerm = SearchBox.Text;
            }
            else if (e.Key == Key.Escape) {
                searchTerm = String.Empty;
                SearchBox.Text = String.Empty;
            }

            if (searchTerm != null) {
                ICommand searchCommand = (ICommand)SearchBox.Tag;
                searchCommand.Execute(searchTerm);
                e.Handled = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            ICommand loadedCommand = (ICommand)Tag;
            loadedCommand.Execute(null);
        }
    }
}