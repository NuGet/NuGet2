using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using PackageExplorerViewModel;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PublishPackageWindow.xaml
    /// </summary>
    public partial class PublishPackageWindow : StandardDialog {
        public PublishPackageWindow() {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        private void OnPublishButtonClick(object sender, RoutedEventArgs e)
        {
            BindingExpression bindingExpression = PublishKey.GetBindingExpression(TextBox.TextProperty);
            bindingExpression.UpdateSource();
            if (!bindingExpression.HasError)
            {
                var viewModel = (PublishPackageViewModel)DataContext;
                viewModel.PushPackage();
            }
        }
    }
}