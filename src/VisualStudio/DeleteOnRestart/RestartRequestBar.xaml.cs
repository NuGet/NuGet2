using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// Interaction logic for RestartRequestBar.xaml
    /// </summary>
    public partial class RestartRequestBar : UserControl
    {
        private readonly IDeleteOnRestartManager _deleteOnRestartManager;
        private readonly IVsShell4 _vsRestarter;

        public RestartRequestBar(IDeleteOnRestartManager deleteOnRestartManager, IVsShell4 vsRestarter)
        {
            InitializeComponent();
            _deleteOnRestartManager = deleteOnRestartManager;
            _vsRestarter = vsRestarter;
        }

        public void CheckForUnsuccessfulUninstall()
        {
            IList<string> packageDirectoriesMarkedForDeletion = _deleteOnRestartManager.GetPackageDirectoriesMarkedForDeletion();
            if (packageDirectoriesMarkedForDeletion != null && packageDirectoriesMarkedForDeletion.Count != 0)
            {
                var message = String.Format(
                    CultureInfo.CurrentCulture,
                    NuGet.VisualStudio.Resources.VsResources.RequestRestartToCompleteUninstall,
                    string.Join(", ", packageDirectoriesMarkedForDeletion));
                RequestRestartMessage.Text = message;
                RestartBar.Visibility = Visibility.Visible;
            }
        }

        private void ExecutedRestart(object sender, EventArgs e)
        {
            _vsRestarter.Restart((uint)__VSRESTARTTYPE.RESTART_Normal);
        }
    }
}
