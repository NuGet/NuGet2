using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NuGet.Client.VisualStudio.UI
{
    public partial class PackageSolutionDetailControl : UserControl
    {
        public PackageManagerControl Control { get; set; }

        public PackageSolutionDetailControl()
        {
            InitializeComponent();
        }

        private void Versions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // !!!
        }

        private void ExecuteOpenLicenseLink(object sender, ExecutedRoutedEventArgs e)
        {
            Hyperlink hyperlink = e.OriginalSource as Hyperlink;
            if (hyperlink != null && hyperlink.NavigateUri != null)
            {
                Control.UI.LaunchExternalLink(hyperlink.NavigateUri);
                e.Handled = true;
            }
        }

        private void _dropdownButton_Clicked(object sender, DropdownButtonClickEventArgs e)
        {
            // !!!
        }
    }
}
