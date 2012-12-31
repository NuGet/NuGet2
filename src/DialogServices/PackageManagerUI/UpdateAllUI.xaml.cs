using System.Windows;
using System.Windows.Controls;

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

        public event RoutedEventHandler UpdateInvoked
        {
            add
            {
                UpdateButton.Click += value;
            }
            remove
            {
                UpdateButton.Click -= value;
            }
        }
    }
}