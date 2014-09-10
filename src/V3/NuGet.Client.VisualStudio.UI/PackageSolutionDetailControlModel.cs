using System.Collections.Generic;
using System.Linq;

namespace NuGet.Client.VisualStudio.UI
{
    // SelectedVersion -> ActionList update -> ProjectList update
    public class PackageSolutionDetailControlModel : PackageDetailControlModel
    {
        // list of projects where the package is installed
        private List<ProjectPackageInfo> _projects;

        private InstalledPackages _installedPackages;

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

                OnPropertyChanged("SelectedAction");
                CreateProjectList();
            }
        }

        public PackageSolutionDetailControlModel(
            UiSearchResultPackage searchResultPackage,
            InstalledPackages installedPackages) :
            base(searchResultPackage, installedVersion: null)
        {
            _installedPackages = installedPackages;

            SelectedVersion = new VersionForDisplay(Package.Version, null);
            CreateActions();
        }

        private void CreateActions()
        {
            _actions = new List<string>();

            // 'update' is applicable if any project has the same package but different
            // version.
            var v = _installedPackages.Projects.Any(
                project =>
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(project, Package.Id);
                    return installedVersion != null &&
                        installedVersion != SelectedVersion.Version;
                });
            if (v)
            {
                _actions.Add(Resources.Resources.Action_Update);
            }

            // !!! What's the difference between consolidate and update?
            // _actions.Add(Resources.Resources.Action_Consolidate);

            // 'install' is applicable if any project does not have package installed.
            v = _installedPackages.Projects.Any(
                project =>
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(project, Package.Id);
                    return installedVersion == null;
                });
            if (v)
            {
                _actions.Add(Resources.Resources.Action_Install);
            }

            // 'install' is applicable if any project has the package with the same version installed.
            v = _installedPackages.Projects.Any(
                project =>
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(project, Package.Id);
                    return installedVersion != null && installedVersion == SelectedVersion.Version;
                });
            if (v)
            {
                _actions.Add(Resources.Resources.Action_Uninstall);
            }

            SelectedAction = _actions[0];
            OnPropertyChanged("Actions");
        }

        protected override void OnSelectedVersionChanged()
        {
            CreateActions();
        }

        private void CreateProjectList()
        {
            _projects = new List<ProjectPackageInfo>();

            if (_selectedAction == Resources.Resources.Action_Update)
            {
                // project list contains projects that have a different version installed
                // !!! should projects with the same version be included ?
                foreach (var project in _installedPackages.Projects)
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(
                        project,
                        Package.Id);
                    if (installedVersion != null &&
                        installedVersion != SelectedVersion.Version)
                    {
                        _projects.Add(new ProjectPackageInfo(project, installedVersion));
                    }
                }
            }
            else if (_selectedAction == Resources.Resources.Action_Consolidate)
            {
                // project list contains projects that have a different version installed
                // !!! should projects with the same version be included ?
                foreach (var project in _installedPackages.Projects)
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(
                        project,
                        Package.Id);
                    if (installedVersion != null &&
                        installedVersion != SelectedVersion.Version)
                    {
                        _projects.Add(new ProjectPackageInfo(project, installedVersion));
                    }
                }
            }
            else if (_selectedAction == Resources.Resources.Action_Install)
            {
                // project list contains projects that either have a different version installed,
                // or does not have the package installed.
                foreach (var project in _installedPackages.Projects)
                {
                    var installedVersion = _installedPackages.GetInstalledVersion(
                        project,
                        Package.Id);
                    if (installedVersion == null ||
                        installedVersion != SelectedVersion.Version)
                    {
                        _projects.Add(new ProjectPackageInfo(project, installedVersion));
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
                        _projects.Add(new ProjectPackageInfo(project, installedVersion));
                    }
                }
            }

            OnPropertyChanged("Projects");
        }
    }
}