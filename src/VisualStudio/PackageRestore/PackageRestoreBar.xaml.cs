using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// Interaction logic for PackageRestoreBar.xaml
    /// </summary>
    public partial class PackageRestoreBar : UserControl
    {
        private readonly IPackageRestoreManager _packageRestoreManager;

        public PackageRestoreBar(IPackageRestoreManager packageRestoreManager)
        {
            InitializeComponent();
            _packageRestoreManager = packageRestoreManager;
            _packageRestoreManager.PackagesMissingStatusChanged += OnPackagesMissingStatusChanged;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // when the control is first loaded, check for missing packages
            _packageRestoreManager.CheckForMissingPackages();
        }

        private void OnPackagesMissingStatusChanged(object sender, PackagesMissingStatusEventArgs e)
        {
            UpdateRestoreBar(e.PackagesMissing);
        }

        private void UpdateRestoreBar(bool packagesMissing)
        {
            RestoreBar.Visibility = packagesMissing ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnRestoreLinkClick(object sender, RoutedEventArgs e)
        {
            RestorePackages();
        }

        private void RestorePackages()
        {
            RestoreButton.IsEnabled = false;

            TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            _packageRestoreManager.RestoreMissingPackages().ContinueWith(
                task =>
                {
                    if (task.IsFaulted)
                    {
                        // re-enable the Restore button to allow users to try again
                        RestoreButton.IsEnabled = true;
                        StatusMessage.Text = VsResources.PackageRestoreErrorTryAgain;
                    }
                }, 
                uiScheduler);
        }
    }
}