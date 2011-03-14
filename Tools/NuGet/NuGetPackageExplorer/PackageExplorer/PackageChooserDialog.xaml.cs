using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using NuGet;
using PackageExplorerViewModel;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PackageChooserDialog.xaml
    /// </summary>
    public partial class PackageChooserDialog : DialogWithNoMinimizeAndMaximize {

        private const string NuGetFeed = "https://go.microsoft.com/fwlink/?LinkID=206669";

        public PackageChooserDialog() {
            InitializeComponent();

            var viewModel = new PackageChooserViewModel();
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            DataContext = viewModel;

            // retrieve the package source from settings.
            string source = Properties.Settings.Default.PackageSource;
            if (String.IsNullOrEmpty(source)) {
                source = NuGetFeed;
            }
            viewModel.PackageSource = source;
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            string name = e.PropertyName;
            if (name.Equals("CurrentSortColumn") || name.Equals("SortDirection")) {
                var viewModel = (PackageChooserViewModel)sender;
                string columnName = viewModel.CurrentSortColumn;
                ListSortDirection direction = (ListSortDirection)viewModel.SortDirection;

                foreach (var column in PackageGridView.Columns) {
                    var header = (GridViewColumnHeader)column.Header;
                    if (header.Tag != null) {
                        AdornerLayer.GetAdornerLayer(header).Remove((Adorner) header.Tag);
                    }

                    if ((string)header.CommandParameter == columnName) {
                        var newAdorner = new SortAdorner(header, direction);
                        header.Tag = newAdorner;
                        AdornerLayer.GetAdornerLayer(header).Add(newAdorner);
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

        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            string searchTerm = null;

            if (e.Key == Key.Enter) {
                searchTerm = SearchBox.Text;
            }
            else if (e.Key == Key.Escape) {
                if (!String.IsNullOrEmpty(SearchBox.Text)) {
                    searchTerm = String.Empty;
                    SearchBox.Text = String.Empty;
                }
            }

            if (searchTerm != null) {
                ICommand searchCommand = (ICommand)SearchBox.Tag;
                searchCommand.Execute(searchTerm);
                e.Handled = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Dispatcher.BeginInvoke(new Action(LoadPackages), DispatcherPriority.Background);
        }

        private void LoadPackages() {
            var loadedCommand = (ICommand)Tag;
            loadedCommand.Execute(null);
        }

        private void Window_Closed(object sender, EventArgs e) {
            // persist the package source
            var viewModel = (PackageChooserViewModel)DataContext;
            Properties.Settings.Default.PackageSource = viewModel.PackageSource;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control) {
                SearchBox.Focus();
                e.Handled = true;
            }
        }
    }
}