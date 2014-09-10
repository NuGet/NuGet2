using System.Collections.Generic;
using System.Linq;

namespace NuGet.Client.VisualStudio.UI
{
    // SelectedAction -> SelectedVersion
    // SelectedVersion -> ProjectList update
    public class PackageSolutionDetailControlModel : PackageDetailControlModel
    {
        // list of projects where the package is installed
        private List<ProjectPackageInfo> _projects;

        private SolutionInstalledPackageList _installedPackages;

        private List<string> _actions;

        public List<ProjectPackageInfo> Projects
        {
            get
            {
                return _projects;
            }
        }

        public List<string> Actions
        {
            get
            {
                return _actions;
            }
        }

        private string _selectedAction;

        public string SelectedAction
        {
            get
            {
                return _selectedAction;
            }
            set
            {
                _selectedAction = value;

                CreateVersions();
                OnPropertyChanged("SelectedAction");
            }
        }

        protected override void OnSelectedVersionChanged()
        {
            CreateProjectList();
        }

        private void CreateVersions()
        {
            if (_selectedAction == Resources.Resources.Action_Consolidate ||
                _selectedAction == Resources.Resources.Action_Uninstall)
            {
                _versions = _installedPackages.Projects
                    .Select(project => _installedPackages.GetInstalledVersion(project, Package.Id))
                    .Where(version => version != null)
                    .OrderByDescending(v => v)
                    .Select(version => new VersionForDisplay(version, string.Empty))
                    .ToList();
            }
            else if (_selectedAction == Resources.Resources.Action_Install ||
                _selectedAction == Resources.Resources.Action_Update)
            {
                _versions = new List<VersionForDisplay>();
                var allVersions = _allPackages.Keys.OrderByDescending(v => v);
                var latestStableVersion = allVersions.FirstOrDefault(v => !v.IsPrerelease);
                if (latestStableVersion != null)
                {
                    _versions.Add(new VersionForDisplay(latestStableVersion, "Latest stable "));
                }

                // add a separator
                if (_versions.Count > 0)
                {
                    _versions.Add(null);
                }

                foreach (var version in allVersions)
                {
                    _versions.Add(new VersionForDisplay(version, string.Empty));
                }
            }

            SelectedVersion = _versions[0];
            OnPropertyChanged("Versions");
        }

        public PackageSolutionDetailControlModel(
            UiSearchResultPackage searchResultPackage,
            SolutionInstalledPackageList installedPackages) :
            base(searchResultPackage, installedVersion: null)
        {
            _installedPackages = installedPackages;
            SelectedVersion = new VersionForDisplay(Package.Version, null);
            CreateActions();
        }

        private void CreateActions()
        {
            // initialize actions
            _actions = new List<string>();
            var canUpdate = _installedPackages.Projects
                .Any(project =>
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(project, Package.Id);
                    return installedVersion != null &&
                        installedVersion != Package.Version;
                });
            if (canUpdate)
            {
                _actions.Add(Resources.Resources.Action_Update);
            }

            var canInstall = _installedPackages.Projects
                .Any(project =>
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(project, Package.Id);
                    return installedVersion == null;
                });
            if (canInstall)
            {
                _actions.Add(Resources.Resources.Action_Install);
            }

            var canUninstall = _installedPackages.Projects
                .Any(project =>
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(project, Package.Id);
                    return installedVersion != null;
                });
            if (canUninstall)
            {
                _actions.Add(Resources.Resources.Action_Uninstall);
            }

            var installedVersions = _installedPackages.Projects
                .Select(project => _installedPackages.GetInstalledVersion(project, Package.Id))
                .Where(version => version != null)
                .Distinct();
            if (installedVersions.Count() >= 2)
            {
                _actions.Add(Resources.Resources.Action_Consolidate);
            }

            SelectedAction = _actions[0];
        }

        private void CreateProjectList()
        {
            _projects = new List<ProjectPackageInfo>();

            if (_selectedAction == Resources.Resources.Action_Update ||
                _selectedAction == Resources.Resources.Action_Consolidate)
            {
                // project list contains projects that have the package installed.
                // The project with the same version installed is included, but disabled.
                foreach (var project in _installedPackages.Projects)
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(
                        project,
                        Package.Id);
                    if (installedVersion != null)
                    {
                        var enabled = installedVersion != SelectedVersion.Version;
                        _projects.Add(new ProjectPackageInfo(project, installedVersion, enabled));
                    }
                }
            }
            else if (_selectedAction == Resources.Resources.Action_Install)
            {
                // project list contains projects that do not have the package installed.
                foreach (var project in _installedPackages.Projects)
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(
                        project,
                        Package.Id);
                    if (installedVersion == null)
                    {
                        _projects.Add(new ProjectPackageInfo(project, installedVersion, enabled: true));
                    }
                }
            }
            else if (_selectedAction == Resources.Resources.Action_Uninstall)
            {
                // project list contains projects that have the same version installed.
                foreach (var project in _installedPackages.Projects)
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(
                        project,
                        Package.Id);
                    if (installedVersion != null &&
                        installedVersion == SelectedVersion.Version)
                    {
                        _projects.Add(new ProjectPackageInfo(project, installedVersion, enabled: true));
                    }
                }
            }

            OnPropertyChanged("Projects");
        }
    }
}