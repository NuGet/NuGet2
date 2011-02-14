using System.Windows;
using PackageExplorerViewModel;
using NuGet;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PackageChooserDialog.xaml
    /// </summary>
    public partial class PackageChooserDialog : DialogWindow {
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
    }
}
