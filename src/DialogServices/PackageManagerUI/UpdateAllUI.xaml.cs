using System.Windows;
using System.Windows.Controls;
using NuGet.Dialog.Providers;

namespace NuGet.Dialog.PackageManagerUI
{
    /// <summary>
    /// Interaction logic for UpdateAllUI.xaml
    /// </summary>
    public partial class UpdateAllUI : UserControl
    {
        public UpdateAllUI()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler UpdateInvoked = delegate {};

        private void CommandBinding_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !OperationCoordinator.IsBusy;
            e.Handled = true;
        }

        private void CommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            UpdateInvoked(this, e);
            e.Handled = true;
        }
    }
}