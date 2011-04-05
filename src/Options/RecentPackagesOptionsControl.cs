using System;
using System.Diagnostics;
using System.Windows.Forms;
using NuGet.VisualStudio;

namespace NuGet.Options {
    public partial class RecentPackagesOptionsControl : UserControl {

        private IRecentPackageRepository _recentPackageRepository;
        private IProductUpdateSettings _productUpdateSettings;

        public RecentPackagesOptionsControl() {
            InitializeComponent();

            _productUpdateSettings = ServiceLocator.GetInstance<IProductUpdateSettings>();
            Debug.Assert(_productUpdateSettings != null);

            _recentPackageRepository = ServiceLocator.GetInstance<IRecentPackageRepository>();
            Debug.Assert(_recentPackageRepository != null);
        }

        private void ClearButton_Click(object sender, EventArgs e) {
            _recentPackageRepository.Clear();
            MessageHelper.ShowInfoMessage(Resources.ShowInfo_ClearRecentPackages, Resources.ShowWarning_Title);
        }

        internal void OnActivated() {
            checkForUpdate.Checked = _productUpdateSettings.ShouldCheckForUpdate;
        }

        internal void OnApply() {
            _productUpdateSettings.ShouldCheckForUpdate = checkForUpdate.Checked;
        }
    }
}