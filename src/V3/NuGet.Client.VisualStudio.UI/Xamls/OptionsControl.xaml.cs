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
using NuGet.VisualStudio;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// The DataContext of this control is DetailControlModel
    /// </summary>
    public partial class OptionsControl : UserControl
    {
        public OptionsControl()
        {
            InitializeComponent();
        }

        private void ExecuteOpenLicenseLink(object sender, ExecutedRoutedEventArgs e)
        {
            Hyperlink hyperlink = e.OriginalSource as Hyperlink;
            if (hyperlink != null && hyperlink.NavigateUri != null)
            {
                var ui = ServiceLocator.GetInstance<IUserInterfaceService>();
                ui.LaunchExternalLink(hyperlink.NavigateUri);
                e.Handled = true;
            }
        }
    }
}
