using System.Collections.Generic;
using System.Linq;

namespace NuGet.Client.VisualStudio.UI
{
    // SelectedAction -> SelectedVersion
    // SelectedVersion -> ProjectList update
    public class PackageSolutionDetailControlModel : PackageDetailControlModel
    {
        // list of projects where the package is installed
        private List<PackageInstallationInfo> _projects;

        private VsSolution _solution;
        private List<string> _actions;

        public List<PackageInstallationInfo> Projects
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
                InstallOptionsVisible = SelectedAction != Resources.Resources.Action_Uninstall;
                CreateVersions();
                OnPropertyChanged("SelectedAction");
            }
        }

        private bool _actionEnabled;

        // Indicates if the action button and preview button is enabled.
        public bool ActionEnabled
        {
            get
            {
                return _actionEnabled;
            }
            set
            {
                _actionEnabled = value;
                OnPropertyChanged("ActionEnabled");
            }
        }

        protected override void OnSelectedVersionChanged()
        {
            CreateProjectList();

            UiDetailedPackage selectedPackage = null;
            if (_allPackages.TryGetValue(SelectedVersion.Version, out selectedPackage))
            {
                Package = selectedPackage;
            }
            else
            {
                Package = null;
            }
        }

        private void CreateVersions()
        {
            if (_selectedAction == Resources.Resources.Action_Consolidate ||
                _selectedAction == Resources.Resources.Action_Uninstall)
            {
                var installedVersions = _solution.Projects
                    .Select(project => project.InstalledPackages.GetInstalledPackage(Package.Id))
                    .ToList();

                installedVersions.Add(_solution.InstalledPackages.GetInstalledPackage(Package.Id));
                _versions = installedVersions.Where(package => package != null)
                    .OrderByDescending(p => p.Identity.Version)
                    .Select(package => new VersionForDisplay(package.Identity.Version, string.Empty))
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

            if (_versions.Count > 0)
            {
                SelectedVersion = _versions[0];
            }
            OnPropertyChanged("Versions");
        }

        public PackageSolutionDetailControlModel(
            UiSearchResultPackage searchResultPackage,
            VsSolution solution) :
            base(searchResultPackage, installedVersion: null)
        {
            _solution = solution;
            SelectedVersion = new VersionForDisplay(Package.Version, null);
            CreateActions();
        }

        private bool CanUpdate()
        {
            var canUpdateInProjects = _solution.Projects
                .Any(project =>
                {
                    return project.InstalledPackages.IsInstalled(Package.Id) && _allPackages.Count >= 2;
                });

            var installedInSolution = _solution.InstalledPackages.IsInstalled(Package.Id);
            var canUpdateInSolution = installedInSolution && _allPackages.Count >= 2;

            return canUpdateInProjects || canUpdateInSolution;
        }

        private bool CanInstall()
        {
            var canInstallInProjects = _solution.Projects
                .Any(project =>
                {
                    return !project.InstalledPackages.IsInstalled(Package.Id);
                });

            var installedInSolution = _solution.InstalledPackages.IsInstalled(Package.Id);

            return !installedInSolution && canInstallInProjects;
        }

        private bool CanUninstall()
        {
            var canUninstallFromProjects = _solution.Projects
                .Any(project =>
                {
                    return project.InstalledPackages.IsInstalled(Package.Id);
                });

            var installedInSolution = _solution.InstalledPackages.IsInstalled(Package.Id);

            return installedInSolution || canUninstallFromProjects;
        }

        private bool CanConsolidate()
        {
            var installedVersions = _solution.Projects
                .Select(project => project.InstalledPackages.GetInstalledPackage(Package.Id))
                .Where(package => package != null)
                .Select(package => package.Identity.Version)
                .Distinct();
            return installedVersions.Count() >= 2;
        }

        // indicates if the install options expander is visible or not
        bool _installOptionsVisible;

        public bool InstallOptionsVisible
        {
            get
            {
                return _installOptionsVisible;
            }
            set
            {
                if (_installOptionsVisible != value)
                {
                    _installOptionsVisible = value;
                    OnPropertyChanged("InstallOptionsVisible");
                }
            }
        }

        private void CreateActions()
        {
            // initialize actions
            _actions = new List<string>();

            if (CanUpdate())
            {
                _actions.Add(Resources.Resources.Action_Update);
            }

            if (CanInstall())
            {
                _actions.Add(Resources.Resources.Action_Install);
            }

            if (CanUninstall())
            {
                _actions.Add(Resources.Resources.Action_Uninstall);
            }

            if (CanConsolidate())
            {
                _actions.Add(Resources.Resources.Action_Consolidate);
            }

            if (_actions.Count > 0)
            {
                SelectedAction = _actions[0];
            }
            else
            {
                InstallOptionsVisible = false;
            }

            OnPropertyChanged("Actions");
        }

        private void CreateProjectList()
        {
            _projects = new List<PackageInstallationInfo>();

            if (_selectedAction == Resources.Resources.Action_Consolidate)
            {
                // project list contains projects that have the package installed.
                // The project with the same version installed is included, but disabled.
                foreach (var project in _solution.Projects)
                {
                    var installed = project.InstalledPackages.GetInstalledPackage(Package.Id);
                    if (installed != null)
                    {
                        var enabled = installed.Identity.Version != SelectedVersion.Version;
                        var info = new PackageInstallationInfo(project, installed.Identity.Version, enabled);
                        _projects.Add(info);
                    }
                }
            }
            else if (_selectedAction == Resources.Resources.Action_Update)
            {
                // project list contains projects/solution that have the package
                // installed. The project/solution with the same version installed
                // is included, but disabled.
                foreach (var project in _solution.Projects)
                {
                    var installed = project.InstalledPackages.GetInstalledPackage(Package.Id);
                    if (installed != null)
                    {
                        var enabled = installed.Identity.Version != SelectedVersion.Version;
                        var info = new PackageInstallationInfo(project, installed.Identity.Version, enabled);
                        _projects.Add(info);
                    }
                }

                var v = _solution.InstalledPackages.GetInstalledPackage(Package.Id);
                if (v != null)
                {
                    var enabled = v.Identity.Version != SelectedVersion.Version;
                    var info = new PackageInstallationInfo(
                        _solution.Name,
                        SelectedVersion.Version,
                        enabled,
                        _solution.Projects.First());
                    _projects.Add(info);
                }
            }
            else if (_selectedAction == Resources.Resources.Action_Install)
            {
                // project list contains projects that do not have the package installed.
                foreach (var project in _solution.Projects)
                {
                    var installed = project.InstalledPackages.GetInstalledPackage(Package.Id);
                    if (installed == null)
                    {
                        var info = new PackageInstallationInfo(project, null, enabled: true);
                        _projects.Add(info);
                    }
                }
            }
            else if (_selectedAction == Resources.Resources.Action_Uninstall)
            {
                // project list contains projects/solution that have the same version installed.
                foreach (var project in _solution.Projects)
                {
                    var installed = project.InstalledPackages.GetInstalledPackage(Package.Id);
                    if (installed != null &&
                        installed.Identity.Version == SelectedVersion.Version)
                    {
                        var info = new PackageInstallationInfo(project, installed.Identity.Version, enabled: true);
                        _projects.Add(info);
                    }
                }

                var v = _solution.InstalledPackages.GetInstalledPackage(Package.Id);
                if (v != null)
                {
                    var enabled = v.Identity.Version == SelectedVersion.Version;
                    var info = new PackageInstallationInfo(
                        _solution.Name,
                        SelectedVersion.Version,
                        enabled,
                        _solution.Projects.First());
                    _projects.Add(info);
                }
            }

            foreach (var p in _projects)
            {
                p.SelectedChanged += (sender, e) =>
                {
                    ActionEnabled = _projects.Any(i => i.Selected);
                };
            }
            ActionEnabled = _projects.Any(i => i.Selected);

            OnPropertyChanged("Projects");
        }

        public void Refresh()
        {
            SelectedVersion = new VersionForDisplay(Package.Version, null);
            CreateActions();
        }
    }
}