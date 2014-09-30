using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NuGet.Client.VisualStudio.UI
{
    // SelectedAction -> SelectedVersion
    // SelectedVersion -> ProjectList update
    public class PackageSolutionDetailControlModel : PackageDetailControlModel
    {
        // list of projects where the package is installed
        private List<PackageInstallationInfo> _projects;

        private VsSolutionInstallationTarget _target;
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
                var installedVersions = _target.TargetProjects
                    .Select(project => project.InstalledPackages.GetInstalledPackage(Package.Id))
                    .ToList();

                installedVersions.Add(_target.InstalledSolutionLevelPackages.GetInstalledPackage(Package.Id));
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

            SelectedVersion = _versions[0];
            OnPropertyChanged("Versions");
        }

        public PackageSolutionDetailControlModel(
            UiSearchResultPackage searchResultPackage,
            InstallationTarget target) :
            base(searchResultPackage, installedVersion: null)
        {
            Debug.Assert(target.IsSolution);

            _target = (VsSolutionInstallationTarget)target;
            SelectedVersion = new VersionForDisplay(Package.Version, null);
            CreateActions();
        }

        private bool CanUpdate()
        {
            var canUpdateInProjects = _target.TargetProjects
                .Any(project =>
                {
                    var installedPackage = project.InstalledPackages.GetInstalledPackage(Package.Id);
                    return installedPackage != null &&
                        _allPackages.Count >= 2;
                });

            var v = _target.InstalledSolutionLevelPackages.GetInstalledPackage(Package.Id);
            var canUpdateInSolution = v != null && _allPackages.Count >= 2;

            return canUpdateInProjects || canUpdateInSolution;
        }

        private bool CanInstall()
        {
            var canInstallInProjects = _target.TargetProjects
                .Any(project =>
                {
                    var installed = project.InstalledPackages.GetInstalledPackage(Package.Id);
                    return installed == null;
                });

            var v = _target.InstalledSolutionLevelPackages.GetInstalledPackage(Package.Id);
            var installedInSolution = v != null;

            return !installedInSolution && canInstallInProjects;
        }

        private bool CanUninstall()
        {
            var canUninstallFromProjects = _target.TargetProjects
                .Any(project =>
                {
                    var installed = project.InstalledPackages.GetInstalledPackage(Package.Id);
                    return installed != null;
                });

            var v = _target.InstalledSolutionLevelPackages.GetInstalledPackage(Package.Id);
            var installedInSolution = v != null;

            return installedInSolution || canUninstallFromProjects;
        }

        private bool CanConsolidate()
        {
            var installedVersions = _target.TargetProjects
                .Select(project => project.InstalledPackages.GetInstalledPackage(Package.Id))
                .Where(version => version != null)
                .Distinct();
            return installedVersions.Count() >= 2;
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

            SelectedAction = _actions[0];
            OnPropertyChanged("Actions");
        }

        private void CreateProjectList()
        {
            _projects = new List<PackageInstallationInfo>();

            if (_selectedAction == Resources.Resources.Action_Consolidate)
            {
                // project list contains projects that have the package installed.
                // The project with the same version installed is included, but disabled.
                foreach (var project in _target.TargetProjects)
                {
                    var installed = project.InstalledPackages.GetInstalledPackage(Package.Id);
                    if (installed != null)
                    {
                        var enabled = installed.Identity.Version != SelectedVersion.Version;
                        _projects.Add(new PackageInstallationInfo(project, installed.Identity.Version, enabled));
                    }
                }
            }
            else if (_selectedAction == Resources.Resources.Action_Update)
            {
                // project list contains projects/solution that have the package
                // installed. The project/solution with the same version installed
                // is included, but disabled.
                foreach (var project in _target.TargetProjects)
                {
                    var installed = project.InstalledPackages.GetInstalledPackage(
                        Package.Id);
                    if (installed != null)
                    {
                        var enabled = installed.Identity.Version != SelectedVersion.Version;
                        _projects.Add(new PackageInstallationInfo(project, installed.Identity.Version, enabled));
                    }
                }

                var v = _target.InstalledSolutionLevelPackages.GetInstalledPackage(Package.Id);
                if (v != null)
                {
                    var enabled = v.Identity.Version != SelectedVersion.Version;
                    _projects.Add(new PackageInstallationInfo(
                        _target.Name,
                        SelectedVersion.Version,
                        enabled,
                        _target.TargetProjects.First()));
                }
            }
            else if (_selectedAction == Resources.Resources.Action_Install)
            {
                // project list contains projects that do not have the package installed.
                foreach (var project in _target.TargetProjects)
                {
                    var installed = project.InstalledPackages.GetInstalledPackage(Package.Id);
                    if (installed == null)
                    {
                        _projects.Add(new PackageInstallationInfo(project, null, enabled: true));
                    }
                }
            }
            else if (_selectedAction == Resources.Resources.Action_Uninstall)
            {
                // project list contains projects/solution that have the same version installed.
                foreach (var project in _target.TargetProjects)
                {
                    var installed = project.InstalledPackages.GetInstalledPackage(Package.Id);
                    if (installed != null &&
                        installed.Identity.Version == SelectedVersion.Version)
                    {
                        _projects.Add(new PackageInstallationInfo(project, installed.Identity.Version, enabled: true));
                    }
                }

                var v = _target.InstalledSolutionLevelPackages.GetInstalledPackage(Package.Id);
                if (v != null)
                {
                    var enabled = v.Identity.Version == SelectedVersion.Version;
                    _projects.Add(new PackageInstallationInfo(
                        _target.Name,
                        SelectedVersion.Version,
                        enabled,
                        _target.TargetProjects.First()));
                }
            }

            OnPropertyChanged("Projects");
        }

        public void Refresh()
        {
            SelectedVersion = new VersionForDisplay(Package.Version, null);
            CreateActions();
        }
    }
}