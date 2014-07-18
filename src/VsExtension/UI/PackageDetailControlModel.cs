using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Tools
{

    // The DataContext of the PackageDetail control is this class
    public class PackageDetailControlModel : INotifyPropertyChanged
    {
        private UiDetailedPackage _package;
        private Dictionary<SemanticVersion, UiDetailedPackage> _allPackages;
        private List<SemanticVersion> _versions;

        public PackageDetailControlModel(UiSearchResultPackage searchResultPackage)
        {
            _allPackages = new Dictionary<SemanticVersion, UiDetailedPackage>();
            foreach (var p in searchResultPackage.AllVersions)
            {
                _allPackages[p.Version] = p;
            }
            _versions = searchResultPackage.AllVersions.Select(p => p.Version).OrderByDescending(v => v).ToList();
            Package = _allPackages[searchResultPackage.Version];
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

        public IEnumerable<SemanticVersion> Versions
        {
            get { return _versions; }
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
