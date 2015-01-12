using System.Collections.Generic;
using System.Linq;

namespace NuGet.Client.VisualStudio.UI
{
    public class PackageSolutionDetailControlModel : DetailControlModel
    {
        // list of projects to be displayed in the UI
        private List<PackageInstallationInfo> _projects;

        private List<PackageInstallationInfo> _allProjects;


        public VsSolution Solution
        {
            get
            {
                return (VsSolution)_target;
            }
        }
        
        public List<PackageInstallationInfo> Projects
        {
            get
            {
                return _projects;
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

        private bool _showAll;

        // Indicates if all projects are displayed or only applicable projectss are displayed
        public bool ShowAll
        {
            get
            {
                return _showAll;
            }
            set
            {
                _showAll = value;
                UpdateProjectList();
            }
        }
        protected override void OnSelectedVersionChanged()
        {
            UpdateProjectList();
        }

        protected override void CreateVersions()
        {
            if (SelectedAction == Resources.Resources.Action_Consolidate ||
                SelectedAction == Resources.Resources.Action_Uninstall)
            {
                var installedVersions = Solution.Projects
                    .Select(project => project.InstalledPackages.GetInstalledPackage(Id))
                    .ToList();

                installedVersions.Add(Solution.InstalledPackages.GetInstalledPackage(Id));
                _versions = installedVersions.Where(package => package != null)
                    .OrderByDescending(p => p.Identity.Version)
                    .Select(package => new VersionForDisplay(package.Identity.Version, string.Empty))
                    .ToList();
            }
            else if (SelectedAction == Resources.Resources.Action_Install ||
                SelectedAction == Resources.Resources.Action_Update)
            {
                _versions = new List<VersionForDisplay>();
                var allVersions = _allPackages.OrderByDescending(v => v);
                var latestStableVersion = allVersions.FirstOrDefault(v => !v.IsPrerelease);
                if (latestStableVersion != null)
                {
                    _versions.Add(new VersionForDisplay(latestStableVersion, 
                        Resources.Resources.Version_LatestStable));
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

        public PackageSolutionDetailControlModel(VsSolution solution) :
            base(solution)
        {
            // create project list
            _allProjects = Solution.Projects.Select(p =>new PackageInstallationInfo(p, null, true))
                .ToList();
            _allProjects.Sort();
            _allProjects.ForEach(p =>
            {
                p.SelectedChanged += (sender, e) =>
                {
                    UpdateActionEnabled();
                };
            });
        }

        private void UpdateActionEnabled()
        {
            ActionEnabled = 
                _projects != null &&
                _projects.Any(i => i.Selected);
        }

        protected override bool CanUpdate()
        {
            var canUpdateInProjects = Solution.Projects
                .Any(project =>
                {
                    return project.InstalledPackages.IsInstalled(Id) && _allPackages.Count >= 2;
                });

            var installedInSolution = Solution.InstalledPackages.IsInstalled(Id);
            var canUpdateInSolution = installedInSolution && _allPackages.Count >= 2;

            return canUpdateInProjects || canUpdateInSolution;
        }

        protected override bool CanInstall()
        {
            var canInstallInProjects = Solution.Projects
                .Any(project =>
                {
                    return !project.InstalledPackages.IsInstalled(Id);
                });

            var installedInSolution = Solution.InstalledPackages.IsInstalled(Id);

            return !installedInSolution && canInstallInProjects;
        }

        protected override bool CanUninstall()
        {
            var canUninstallFromProjects = Solution.Projects
                .Any(project =>
                {
                    return project.InstalledPackages.IsInstalled(Id);
                });

            var installedInSolution = Solution.InstalledPackages.IsInstalled(Id);

            return installedInSolution || canUninstallFromProjects;
        }

        protected override bool CanConsolidate()
        {
            var installedVersions = Solution.Projects
                .Select(project => project.InstalledPackages.GetInstalledPackage(Id))
                .Where(package => package != null)
                .Select(package => package.Identity.Version)
                .Distinct();
            return installedVersions.Count() >= 2;
        }

        private void UpdateProjectList()
        {
            // update properties of _allProject list
            _allProjects.ForEach(p =>
            {
                var installed = p.Project.InstalledPackages.GetInstalledPackage(Id);
                if (installed != null)
                {
                    p.Version = installed.Identity.Version;
                }
                else
                {
                    p.Version = null;
                }
            });


            if (SelectedAction == Resources.Resources.Action_Consolidate)
            {
                // only projects that have the package installed are selected.
                // The project with the same version installed is selected, but disabled.
                _allProjects.ForEach(p =>
                {
                    var installed = p.Project.InstalledPackages.GetInstalledPackage(Id);
                    if (installed != null)
                    {
                        p.Selected = true;
                        p.Enabled = installed.Identity.Version != SelectedVersion.Version;
                    }
                    else
                    {
                        p.Selected = false;
                        p.Enabled = false;                        
                    }
                });
            }
            else if (SelectedAction == Resources.Resources.Action_Update)
            {
                // only projects that have the package of a different version installed are enabled
                _allProjects.ForEach(p =>
                {
                    var installed = p.Project.InstalledPackages.GetInstalledPackage(Id);
                    p.Enabled = installed != null &&
                        installed.Identity.Version != SelectedVersion.Version;
                    p.Selected = p.Enabled;
                });
            }
            else if (SelectedAction == Resources.Resources.Action_Install)
            {
                // only projects that do not have the package installed are enabled
                _allProjects.ForEach(p =>
                {
                    var installed = p.Project.InstalledPackages.GetInstalledPackage(Id);
                    p.Enabled = installed == null;
                    p.Selected = p.Enabled;
                });
            }
            else if (SelectedAction == Resources.Resources.Action_Uninstall)
            {
                // only projects that have the selected version installed are enabled
                _allProjects.ForEach(p =>
                {
                    var installed = p.Project.InstalledPackages.GetInstalledPackage(Id);
                    p.Enabled = installed != null &&
                        installed.Identity.Version == SelectedVersion.Version;
                    p.Selected = p.Enabled;
                });
            }

            if (ShowAll)
            {
                _projects = _allProjects;
            }
            else
            {
                _projects = _allProjects.Where(
                    p => p.Enabled || p.Selected).ToList();
            }
            UpdateActionEnabled();

            OnPropertyChanged("Projects");
        }

        internal void UncheckAllProjects()
        {
            _projects.ForEach(p =>
            {
                if (p.Enabled)
                {
                    p.Selected = false;
                }
            });

            OnPropertyChanged("Projects");
        }

        internal void CheckAllProjects()
        {
            _projects.ForEach(p =>
            {
                if (p.Enabled)
                {
                    p.Selected = true;
                }
            });

            OnPropertyChanged("Projects");
        }
    }
}