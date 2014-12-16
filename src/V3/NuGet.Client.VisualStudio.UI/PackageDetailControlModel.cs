using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using NuGet.Client.Installation;
using NuGet.Versioning;
using Resx = NuGet.Client.VisualStudio.UI.Resources;

namespace NuGet.Client.VisualStudio.UI
{
    // The DataContext of the PackageDetail control is this class
    // It has two mode: Project, or Solution
    public class PackageDetailControlModel : DetailControlModel
    {
        public PackageDetailControlModel(
            InstallationTarget target,
            UiSearchResultPackage searchResultPackage)
            : base(target, searchResultPackage)
        {
            Debug.Assert(!target.IsSolution);
            UpdateInstalledVersion();
        }

        private void UpdateInstalledVersion()
        {
            var installed = _target.InstalledPackages.GetInstalledPackage(_searchResultPackage.Id);
            if (installed != null)
            {
                InstalledVersion = string.Format(
                    CultureInfo.CurrentCulture,
                    Resx.Resources.Text_InstalledVersion,
                    installed.Identity.Version.ToNormalizedString());
            }
            else
            {
                InstalledVersion = null;
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            UpdateInstalledVersion();
        }

        protected override bool CanUpdate()
        {
            return _target.InstalledPackages.IsInstalled(Id) &&
                _allPackages.Count >= 2;
        }

        protected override bool CanInstall()
        {
            return !_target.InstalledPackages.IsInstalled(Id);
        }

        protected override bool CanUninstall()
        {
            return _target.InstalledPackages.IsInstalled(Id);
        }

        protected override bool CanConsolidate()
        {
            return false;
        }

        protected override void CreateVersions()
        {
            _versions = new List<VersionForDisplay>();
            var installedVersion = _target.InstalledPackages.GetInstalledPackage(Id);
            var allVersions = _allPackages.OrderByDescending(v => v);
            var latestStableVersion = allVersions.FirstOrDefault(v => !v.IsPrerelease);

            if (SelectedAction == Resx.Resources.Action_Uninstall)
            {
                _versions.Add(new VersionForDisplay(installedVersion.Identity.Version, string.Empty));
            }
            else if (SelectedAction == Resx.Resources.Action_Install)
            {
                if (latestStableVersion != null)
                {
                    _versions.Add(new VersionForDisplay(latestStableVersion, Resx.Resources.Version_LatestStable));

                    // add a separator
                    _versions.Add(null);
                }

                foreach (var version in allVersions)
                {
                    _versions.Add(new VersionForDisplay(version, string.Empty));
                }
            }
            else
            {
                // update
                if (latestStableVersion != null &&
                    latestStableVersion != installedVersion.Identity.Version)
                {
                    _versions.Add(new VersionForDisplay(latestStableVersion, Resx.Resources.Version_LatestStable));

                    // add a separator
                    _versions.Add(null);
                }

                foreach (var version in allVersions.Where(v => v != installedVersion.Identity.Version))
                {
                    _versions.Add(new VersionForDisplay(version, string.Empty));
                }
            }

            if (_versions.Count > 0)
            {
                SelectedVersion = _versions[0];
            }

            OnPropertyChanged("Versions");
        }

        protected override void OnSelectedVersionChanged()
        {
            // no-op
        }

        private string _installedVersion;

        public string InstalledVersion
        {
            get
            {
                return _installedVersion;
            }
            private set
            {
                _installedVersion = value;
                OnPropertyChanged("InstalledVersion");
            }
        }
    }
}