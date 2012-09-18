using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// Interaction logic for RestartRequestBar.xaml
    /// </summary>
    public partial class RestartRequestBar : UserControl
    {
        private readonly RoutedCommand RestartVisualStudio = new RoutedCommand();
        private readonly IDeleteOnRestartManager _deleteOnRestartManager;
        private readonly IVsShell4 _vsRestarter;

        public RestartRequestBar(IDeleteOnRestartManager deleteOnRestartManager, IVsShell4 vsRestarter)
        {
            InitializeComponent();
            _deleteOnRestartManager = deleteOnRestartManager;
            _vsRestarter = vsRestarter;
        }

        public void NotifyOnUnsuccessfulUninstall(object sender, EventArgs e)
        {
            if (_deleteOnRestartManager.PackageDirectoriesAreMarkedForDeletion)
            {
                RestartBar.Visibility = Visibility.Visible;
            }
        }

        private void ExecutedRestart(object sender, EventArgs e)
        {
            _vsRestarter.Restart((uint)__VSRESTARTTYPE.RESTART_Normal);
        }
    }
}
