using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.UI
{
    public class VersionForDisplay
    {
        string _additionalInfo;

        public VersionForDisplay(
            SemanticVersion version,
            string additionalInfo)
        {
            Version = version;
            _additionalInfo = additionalInfo;
        }

        public SemanticVersion Version
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

    // The DataContext of the PackageDetail control is this class
    public class PackageDetailControlModel : INotifyPropertyChanged
    {
        private UiDetailedPackage _package;
        private Dictionary<SemanticVersion, UiDetailedPackage> _allPackages;

        // used for data binding
        private List<VersionForDisplay> _versions;

        public PackageDetailControlModel(
            UiSearchResultPackage searchResultPackage,
            SemanticVersion installedVersion)
        {
            _allPackages = new Dictionary<SemanticVersion, UiDetailedPackage>();
            foreach (var p in searchResultPackage.AllVersions)
            {
                _allPackages[p.Version] = p;
            }
            
            Package = _allPackages[searchResultPackage.Version];
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

        public void CreateVersions(SemanticVersion installedVersion)
        {
            _versions = new List<VersionForDisplay>();

            if (installedVersion != null)
            {
                _versions.Add(new VersionForDisplay(installedVersion, "Installed "));
            }

            var allVersions = _allPackages.Keys.OrderByDescending(v => v);
            var latestStableVersion = allVersions.FirstOrDefault(v => String.IsNullOrEmpty(v.SpecialVersion));
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

        public void SelectVersion(SemanticVersion version)
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
