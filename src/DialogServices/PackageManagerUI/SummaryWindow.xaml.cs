using System.Windows;
using Microsoft.VisualStudio.PlatformUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.PackageManagerUI
{
    /// <summary>
    /// Interaction logic for SummaryWindow.xaml
    /// </summary>
    public partial class SummaryWindow : VsDialogWindow
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
