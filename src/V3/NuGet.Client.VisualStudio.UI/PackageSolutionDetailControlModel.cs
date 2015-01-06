//using System.Collections.Generic;
//using System.Linq;

//namespace NuGet.Client.VisualStudio.UI
//{
//    public class PackageSolutionDetailControlModel : DetailControlModel
//    {
//        // list of projects where the package is installed
//        private List<PackageInstallationInfo> _projects;

//        public VsSolution Solution
//        {
//            get
//            {
//                return (VsSolution)_target;
//            }
//        }
        
//        public List<PackageInstallationInfo> Projects
//        {
//            get
//            {
//                return _projects;
//            }
//        }

//        private bool _actionEnabled;

//        // Indicates if the action button and preview button is enabled.
//        public bool ActionEnabled
//        {
//            get
//            {
//                return _actionEnabled;
//            }
//            set
//            {
//                _actionEnabled = value;
//                OnPropertyChanged("ActionEnabled");
//            }
//        }

//        protected override void OnSelectedVersionChanged()
//        {
//            CreateProjectList();
//        }

//        protected override void CreateVersions()
//        {
//            if (SelectedAction == Resources.Resources.Action_Consolidate ||
//                SelectedAction == Resources.Resources.Action_Uninstall)
//            {
//                var installedVersions = Solution.Projects
//                    .Select(project => project.InstalledPackages.GetInstalledPackage(Id))
//                    .ToList();

//                installedVersions.Add(Solution.InstalledPackages.GetInstalledPackage(Id));
//                _versions = installedVersions.Where(package => package != null)
//                    .OrderByDescending(p => p.Identity.Version)
//                    .Select(package => new VersionForDisplay(package.Identity.Version, string.Empty))
//                    .ToList();
//            }
//            else if (SelectedAction == Resources.Resources.Action_Install ||
//                SelectedAction == Resources.Resources.Action_Update)
//            {
//                _versions = new List<VersionForDisplay>();
//                var allVersions = _allPackages.OrderByDescending(v => v);
//                var latestStableVersion = allVersions.FirstOrDefault(v => !v.IsPrerelease);
//                if (latestStableVersion != null)
//                {
//                    _versions.Add(new VersionForDisplay(latestStableVersion, 
//                        Resources.Resources.Version_LatestStable));
//                }

//                // add a separator
//                if (_versions.Count > 0)
//                {
//                    _versions.Add(null);
//                }

//                foreach (var version in allVersions)
//                {
//                    _versions.Add(new VersionForDisplay(version, string.Empty));
//                }
//            }

//            if (_versions.Count > 0)
//            {
//                SelectedVersion = _versions[0];
//            }
//            OnPropertyChanged("Versions");
//        }

//        public PackageSolutionDetailControlModel(
//            VsSolution solution,
//            UiSearchResultPackage searchResultPackage) :
//            base(solution, searchResultPackage)
//        {
//        }

//        protected override bool CanUpdate()
//        {
//            var canUpdateInProjects = Solution.Projects
//                .Any(project =>
//                {
//                    return project.InstalledPackages.IsInstalled(Id) && _allPackages.Count >= 2;
//                });

//            var installedInSolution = Solution.InstalledPackages.IsInstalled(Id);
//            var canUpdateInSolution = installedInSolution && _allPackages.Count >= 2;

//            return canUpdateInProjects || canUpdateInSolution;
//        }

//        protected override bool CanInstall()
//        {
//            var canInstallInProjects = Solution.Projects
//                .Any(project =>
//                {
//                    return !project.InstalledPackages.IsInstalled(Id);
//                });

//            var installedInSolution = Solution.InstalledPackages.IsInstalled(Id);

//            return !installedInSolution && canInstallInProjects;
//        }

//        protected override bool CanUninstall()
//        {
//            var canUninstallFromProjects = Solution.Projects
//                .Any(project =>
//                {
//                    return project.InstalledPackages.IsInstalled(Id);
//                });

//            var installedInSolution = Solution.InstalledPackages.IsInstalled(Id);

//            return installedInSolution || canUninstallFromProjects;
//        }

//        protected override bool CanConsolidate()
//        {
//            var installedVersions = Solution.Projects
//                .Select(project => project.InstalledPackages.GetInstalledPackage(Id))
//                .Where(package => package != null)
//                .Select(package => package.Identity.Version)
//                .Distinct();
//            return installedVersions.Count() >= 2;
//        }

//        private void CreateProjectList()
//        {
//            _projects = new List<PackageInstallationInfo>();

//            if (SelectedAction == Resources.Resources.Action_Consolidate)
//            {
//                // project list contains projects that have the package installed.
//                // The project with the same version installed is included, but disabled.
//                foreach (var project in Solution.Projects)
//                {
//                    var installed = project.InstalledPackages.GetInstalledPackage(Id);
//                    if (installed != null)
//                    {
//                        var enabled = installed.Identity.Version != SelectedVersion.Version;
//                        var info = new PackageInstallationInfo(project, installed.Identity.Version, enabled);
//                        _projects.Add(info);
//                    }
//                }
//            }
//            else if (SelectedAction == Resources.Resources.Action_Update)
//            {
//                // project list contains projects/solution that have the package
//                // installed. The project/solution with the same version installed
//                // is included, but disabled.
//                foreach (var project in Solution.Projects)
//                {
//                    var installed = project.InstalledPackages.GetInstalledPackage(Id);
//                    if (installed != null)
//                    {
//                        var enabled = installed.Identity.Version != SelectedVersion.Version;
//                        var info = new PackageInstallationInfo(project, installed.Identity.Version, enabled);
//                        _projects.Add(info);
//                    }
//                }

//                var v = Solution.InstalledPackages.GetInstalledPackage(Id);
//                if (v != null)
//                {
//                    var enabled = v.Identity.Version != SelectedVersion.Version;
//                    var info = new PackageInstallationInfo(
//                        Solution.Name,
//                        SelectedVersion.Version,
//                        enabled,
//                        Solution.Projects.First());
//                    _projects.Add(info);
//                }
//            }
//            else if (SelectedAction == Resources.Resources.Action_Install)
//            {
//                // project list contains projects that do not have the package installed.
//                foreach (var project in Solution.Projects)
//                {
//                    var installed = project.InstalledPackages.GetInstalledPackage(Id);
//                    if (installed == null)
//                    {
//                        var info = new PackageInstallationInfo(project, null, enabled: true);
//                        _projects.Add(info);
//                    }
//                }
//            }
//            else if (SelectedAction == Resources.Resources.Action_Uninstall)
//            {
//                // project list contains projects/solution that have the same version installed.
//                foreach (var project in Solution.Projects)
//                {
//                    var installed = project.InstalledPackages.GetInstalledPackage(Id);
//                    if (installed != null &&
//                        installed.Identity.Version == SelectedVersion.Version)
//                    {
//                        var info = new PackageInstallationInfo(project, installed.Identity.Version, enabled: true);
//                        _projects.Add(info);
//                    }
//                }

//                var v = Solution.InstalledPackages.GetInstalledPackage(Id);
//                if (v != null)
//                {
//                    var enabled = v.Identity.Version == SelectedVersion.Version;
//                    var info = new PackageInstallationInfo(
//                        Solution.Name,
//                        SelectedVersion.Version,
//                        enabled,
//                        Solution.Projects.First());
//                    _projects.Add(info);
//                }
//            }

//            foreach (var p in _projects)
//            {
//                p.SelectedChanged += (sender, e) =>
//                {
//                    ActionEnabled = _projects.Any(i => i.Selected);
//                };
//            }
//            ActionEnabled = _projects.Any(i => i.Selected);

//            OnPropertyChanged("Projects");
//        }
//    }
//}