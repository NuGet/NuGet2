using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace NuGet.Dialog.PackageManagerUI
{
    /// <summary>
    /// Interaction logic for SummaryWindow.xaml
    /// </summary>
    public partial class SummaryWindow : DialogWindow
    {
        public SummaryWindow()
        {
            InitializeComponent();
        }

        private void OKButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
