using System.Windows;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PublishPackageWindow.xaml
    /// </summary>
    public partial class PublishPackageWindow : DialogWithNoMinimizeAndMaximize {
        public PublishPackageWindow() {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }
}
