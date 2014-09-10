using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using NuGet.Versioning;

namespace NuGet.Client.VisualStudio.UI
{
    public class VersionForDisplay
    {
        private string _additionalInfo;

        public VersionForDisplay(
            NuGetVersion version,
            string additionalInfo)
        {
            Version = version;
            _additionalInfo = additionalInfo;
        }

        public NuGetVersion Version
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return _additionalInfo + Version.ToString();
        }

        public override bool Equals(object obj)
        {
            var other = obj as VersionForDisplay;
            return other != null && other.Version == Version;
        }

        public override int GetHashCode()
        {
            return Version.GetHashCode();
        }
    }

    // Represents the version of a package that is installed in the project
    public class ProjectPackageInfo
    {
        public EnvDTE.Project Project
        { 
            get; 
            private set; 
        }

        public SemanticVersion Version
        {
            get;
            private set;
        }

        public bool Selected
        {
            get;
            set;
        }

        private string _projectName;

        public ProjectPackageInfo(EnvDTE.Project project, SemanticVersion version)
        {
            Debug.Assert(project != null);

            Project = project;
            _projectName = Project.Name;
            Version = version;
        }

        public override string ToString()
        {
            if (Version == null)
            {
                return _projectName;
            }
            else
            {
                return string.Format("{0} ({1})", _projectName,
                    Version.ToString());
            }
        }
    }

    // Used to check if a package is installed in a project
    public class InstalledPackages
    {
        Dictionary<EnvDTE.Project, Dictionary<string, SemanticVersion>> _installedPackages;

        public InstalledPackages()
        {
            _installedPackages = new Dictionary<EnvDTE.Project, Dictionary<string, SemanticVersion>>();
        }

        public IEnumerable<EnvDTE.Project> Projects
        {
            get
            {
                return _installedPackages.Keys;
            }
        }

        public void AddProject(EnvDTE.Project project)
        {
            _installedPackages[project] = new Dictionary<string, SemanticVersion>(StringComparer.OrdinalIgnoreCase);
        }

        public void Add(EnvDTE.Project project, string packageId, SemanticVersion version)
        {
            _installedPackages[project][packageId] = version;
        }

        public SemanticVersion GetInstalledVersion(EnvDTE.Project project, string packageId)
        {
            Dictionary<string, SemanticVersion> d;
            if (!_installedPackages.TryGetValue(project, out d))
            {
                return null;
            }

            SemanticVersion version;
            if (!d.TryGetValue(packageId, out version))
            {
                return null;
            }

            return version;
        }
    }    

    // The DataContext of the PackageDetail control is this class
    // It has two mode: Project, or Solution
    public class PackageDetailControlModel : INotifyPropertyChanged
    {
        private UiDetailedPackage _package;
        private Dictionary<NuGetVersion, UiDetailedPackage> _allPackages;

        // used for data binding
        private List<VersionForDisplay> _versions;

        public PackageDetailControlModel(
            UiSearchResultPackage searchResultPackage,
            NuGetVersion installedVersion)
        {
            _allPackages = new Dictionary<NuGetVersion, UiDetailedPackage>();
            foreach (var p in searchResultPackage.AllVersions)
            {
                _allPackages[p.Version] = p;
            }

            _package = _allPackages[searchResultPackage.Version];
            CreateVersions(installedVersion);
        }

        public UiDetailedPackage Package
        {
            get { return _package; }
            set
            {
                if (_package != value)
                {
                    _package = value;
                    OnPropertyChanged("Package");
                }
            }
        }

        public void CreateVersions(NuGetVersion installedVersion)
        {
            _versions = new List<VersionForDisplay>();

            if (installedVersion != null)
            {
                _versions.Add(new VersionForDisplay(installedVersion, "Installed "));
            }

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
            OnPropertyChanged("Versions");
        }

        public List<VersionForDisplay> Versions
        {
            get
            {
                return _versions;
            }
        }

        private VersionForDisplay _selectedVersion;

        protected virtual void OnSelectedVersionChanged()
        {
        }

        public VersionForDisplay SelectedVersion
        {
            get
            {
                return _selectedVersion;
            }
            set
            {
                _selectedVersion = value;
                OnSelectedVersionChanged();
                OnPropertyChanged("SelectedVersion");
            }
        }

        public void SelectVersion(NuGetVersion version)
        {
            if (version == null)
            {
                return;
            }

            Package = _allPackages[version];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}