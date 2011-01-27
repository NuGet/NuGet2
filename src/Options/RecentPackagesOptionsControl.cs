using System;
using System.Windows.Forms;
using NuGet.VisualStudio;

namespace NuGet.Options {
    public partial class RecentPackagesOptionsControl : UserControl {
        public RecentPackagesOptionsControl() {
            InitializeComponent();
        }

        private void ClearButton_Click(object sender, EventArgs e) {
            var recentPackageRepository = ServiceLocator.GetInstance<IRecentPackageRepository>();
            if (recentPackageRepository != null) {
                recentPackageRepository.Clear();
                MessageHelper.ShowInfoMessage(Resources.ShowInfo_ClearRecentPackages, Resources.ShowWarning_Title);
            }
        }
    }
}