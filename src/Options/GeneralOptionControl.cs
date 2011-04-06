using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using NuGet.VisualStudio;

namespace NuGet.Options {
    public partial class GeneralOptionControl : UserControl {

        private IRecentPackageRepository _recentPackageRepository;
        private IProductUpdateSettings _productUpdateSettings;

        public GeneralOptionControl() {
            InitializeComponent();

            _productUpdateSettings = ServiceLocator.GetInstance<IProductUpdateSettings>();
            Debug.Assert(_productUpdateSettings != null);

            _recentPackageRepository = ServiceLocator.GetInstance<IRecentPackageRepository>();
            Debug.Assert(_recentPackageRepository != null);
        }

        private void OnClearRecentPackagesClick(object sender, EventArgs e) {
            _recentPackageRepository.Clear();
            MessageHelper.ShowInfoMessage(Resources.ShowInfo_ClearRecentPackages, Resources.ShowWarning_Title);
        }

        internal void OnActivated() {
            checkForUpdate.Checked = _productUpdateSettings.ShouldCheckForUpdate;
            browsePackageCacheButton.Enabled = clearPackageCacheButton.Enabled = Directory.Exists(MachineCache.Default.Source);
        }

        internal void OnApply() {
            _productUpdateSettings.ShouldCheckForUpdate = checkForUpdate.Checked;
        }

        private void OnClearPackageCacheClick(object sender, EventArgs e) {
            MachineCache.Default.Clear();
            MessageHelper.ShowInfoMessage(Resources.ShowInfo_ClearRecentPackages, Resources.ShowWarning_Title);
        }

        private void OnBrowsePackageCacheClick(object sender, EventArgs e) {
            if (Directory.Exists(MachineCache.Default.Source)) {
                Process.Start(MachineCache.Default.Source);
            }
        }
    }
}