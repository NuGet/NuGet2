using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using NuGet;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PackageChooserDialog.xaml
    /// </summary>
    public partial class PackageChooserDialog : StandardDialog {

        public string SortColumn
        {
            get { return (string)GetValue(SortColumnProperty); }
            set { SetValue(SortColumnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SortColumn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SortColumnProperty =
            DependencyProperty.Register("SortColumn", typeof(string), typeof(PackageChooserDialog), null);

        public ListSortDirection SortDirection
        {
            get { return (ListSortDirection)GetValue(SortDirectionProperty); }
            set { SetValue(SortDirectionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SortDirection.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SortDirectionProperty =
            DependencyProperty.Register("SortDirection", typeof(ListSortDirection), typeof(PackageChooserDialog), null);

        public int SortCounter
        {
            get { return (int)GetValue(SortCounterProperty); }
            set { SetValue(SortCounterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SortCounter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SortCounterProperty =
            DependencyProperty.Register(
                "SortCounter", 
                typeof(int), 
                typeof(PackageChooserDialog), 
                new UIPropertyMetadata(0, new PropertyChangedCallback(OnSortCounterPropertyChanged)));

        private static void OnSortCounterPropertyChanged(object sender, DependencyPropertyChangedEventArgs args) {
            var dialog = (PackageChooserDialog)sender;
            dialog.RedrawSortGlyph();
        }

        public PackageChooserDialog() {
            InitializeComponent();

            SetBinding(SortColumnProperty, new Binding("SortColumn") { Mode = BindingMode.OneWay });
            SetBinding(SortDirectionProperty, new Binding("SortDirection") { Mode = BindingMode.OneWay });
            SetBinding(SortCounterProperty, new Binding("SortCounter") { Mode = BindingMode.OneWay });
        }

        private void RedrawSortGlyph() {
            foreach (var column in PackageGridView.Columns) {
                var header = (GridViewColumnHeader)column.Header;
                if (header.Tag != null) {
                    AdornerLayer layer = AdornerLayer.GetAdornerLayer(header);
                    if (layer != null)
                    {
                        layer.Remove((Adorner)header.Tag);
                    }
                }

                if ((string)header.CommandParameter == SortColumn) {
                    var newAdorner = new SortAdorner(header, SortDirection);
                    header.Tag = newAdorner;

                    AdornerLayer layer = AdornerLayer.GetAdornerLayer(header);
                    if (layer != null)
                    {
                        layer.Add(newAdorner);
                    }
                }
            }
        }

        public PackageInfo SelectedPackage {
            get {
                return PackageGrid.SelectedItem as PackageInfo;
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
                Search(searchTerm);
                e.Handled = true;
            }
        }

        private void Search(string searchTerm) {
            ICommand searchCommand = (ICommand)SearchBox.Tag;
            searchCommand.Execute(searchTerm);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            AdjustSearchBox();

            Dispatcher.BeginInvoke(new Action(LoadPackages), DispatcherPriority.Background);
        }

        private void AdjustSearchBox() {
            // HACK: Make space for the search image inside the search box
            if (SearchBox.Template != null) {
                var contentHost = SearchBox.Template.FindName("PART_ContentHost", SearchBox) as FrameworkElement;
                if (contentHost != null) {
                    contentHost.Margin = new Thickness(0, 0, 20, 0);
                    contentHost.Width = 150;
                }
            }
        }

        private void LoadPackages() {
            var loadedCommand = (ICommand)Tag;
            loadedCommand.Execute(null);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control) {
                SearchBox.Focus();
                e.Handled = true;
            }
        }
    }
}