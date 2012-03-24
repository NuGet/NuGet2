using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using NuGet.VisualStudio;

namespace NuGet.Options
{
    public partial class GeneralOptionControl : UserControl
    {
        private readonly IRecentPackageRepository _recentPackageRepository;
        private readonly IProductUpdateSettings _productUpdateSettings;
        private readonly PackageRestoreConsent _packageRestoreConsent;

        public GeneralOptionControl()
        {
            InitializeComponent();

            _productUpdateSettings = ServiceLocator.GetInstance<IProductUpdateSettings>();
            Debug.Assert(_productUpdateSettings != null);

            _recentPackageRepository = ServiceLocator.GetInstance<IRecentPackageRepository>();
            Debug.Assert(_recentPackageRepository != null);

            ISettings configSettings = ServiceLocator.GetInstance<ISettings>();
            _packageRestoreConsent = new PackageRestoreConsent(configSettings);

            if (!VsVersionHelper.IsVisualStudio2010)
            {
                // Starting from VS11, we don't need to check for updates anymore because VS will do it.
                Controls.Remove(updatePanel);
            }
        }

        private void OnClearRecentPackagesClick(object sender, EventArgs e)
        {
            _recentPackageRepository.Clear();
            MessageHelper.ShowInfoMessage(Resources.ShowInfo_ClearRecentPackages, Resources.ShowWarning_Title);
        }

        internal void OnActivated()
        {
            checkForUpdate.Checked = _productUpdateSettings.ShouldCheckForUpdate;
            browsePackageCacheButton.Enabled = clearPackageCacheButton.Enabled = Directory.Exists(MachineCache.Default.Source);
            packageRestoreConsentCheckBox.Checked = _packageRestoreConsent.IsGranted;
        }

        internal void OnApply()
        {
            _productUpdateSettings.ShouldCheckForUpdate = checkForUpdate.Checked;
            _packageRestoreConsent.IsGranted = packageRestoreConsentCheckBox.Checked;
        }

        private void OnClearPackageCacheClick(object sender, EventArgs e)
        {
            MachineCache.Default.Clear();
            MessageHelper.ShowInfoMessage(Resources.ShowInfo_ClearPackageCache, Resources.ShowWarning_Title);
        }

        private void OnBrowsePackageCacheClick(object sender, EventArgs e)
        {
            if (Directory.Exists(MachineCache.Default.Source))
            {
                Process.Start(MachineCache.Default.Source);
            }
        }
    }
}