using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Tools
{
    public class UiDetailedPackage
    {
        public string Id { get; set; }

        public SemanticVersion Version { get; set; }

        public string Summary { get; set; }

        public string Description { get; set; }

        public string Authors { get; set; }

        public string Owners { get; set; }

        public Uri IconUrl { get; set; }

        public Uri LicenseUrl { get; set; }

        public Uri ProjectUrl { get; set; }

        public string Tags { get; set; }

        public int DownloadCount { get; set; }

        public DateTimeOffset? Published { get; set; }

        public IEnumerable<PackageDependencySet> DependencySets { get; set; }

        // This property is used by data binding to display text "No dependencies"
        public bool NoDependencies { get; set; }

        public IPackage Package { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", Id, Version);
        }
    }

    public enum PackageStatus
    {
        NotInstalled,
        Installed,
        UpdateAvailable
    }

    public class UiSearchResultPackage : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Id { get; set; }

        public SemanticVersion Version { get; set; }

        public string Summary { get; set; }

        private PackageStatus _status;

        public PackageStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    _status = value;
                }
                OnPropertyChanged("Status");
            }
        }

        public Uri IconUrl { get; set; }

        public IEnumerable<UiDetailedPackage> AllVersions { get; set; }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }
    }
}
