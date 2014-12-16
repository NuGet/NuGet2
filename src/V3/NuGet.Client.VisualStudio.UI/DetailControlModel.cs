using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Installation;
using NuGet.Versioning;
using Resx = NuGet.Client.VisualStudio.UI.Resources;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// The base class of PackageDetailControlModel and PackageSolutionDetailControlModel.
    /// When user selects an action, this triggers version list update.
    /// </summary>
    public abstract class DetailControlModel : INotifyPropertyChanged
    {
        protected InstallationTarget _target;
        protected List<NuGetVersion> _allPackages;
        protected UiSearchResultPackage _searchResultPackage;

        private Dictionary<NuGetVersion, UiPackageMetadata> _metadataDict;

        public DetailControlModel(
            InstallationTarget target,
            UiSearchResultPackage searchResultPackage)
        {
            _target = target;
            _searchResultPackage = searchResultPackage;
            _allPackages = new List<NuGetVersion>(searchResultPackage.Versions);
            _options = new UI.Options();
            CreateActions();
        }

        public virtual void Refresh()
        {
            CreateActions();
        }

        protected abstract bool CanUpdate();
        protected abstract bool CanInstall();
        protected abstract bool CanUninstall();
        protected abstract bool CanConsolidate();

        // Create the _actions list
        protected void CreateActions()
        {
            _actions = new List<string>();

            if (CanInstall())
            {
                _actions.Add(Resources.Resources.Action_Install);
            }

            if (CanUpdate())
            {
                _actions.Add(Resources.Resources.Action_Update);
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
                SelectedActionIsInstall = false;
            }

            OnPropertyChanged("Actions");
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

        private List<string> _actions;

        public List<string> Actions
        {
            get
            {
                return _actions;
            }
        }

        public string Id
        {
            get
            {
                return _searchResultPackage.Id;
            }
        }

        public Uri IconUrl
        {
            get
            {
                return _searchResultPackage.IconUrl;
            }
        }

        private UiPackageMetadata _packageMetadata;

        public UiPackageMetadata PackageMetadata
        {
            get { return _packageMetadata; }
            set
            {
                if (_packageMetadata != value)
                {
                    _packageMetadata = value;
                    OnPropertyChanged("PackageMetadata");
                }
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
                SelectedActionIsInstall = SelectedAction != Resources.Resources.Action_Uninstall;
                CreateVersions();
                OnPropertyChanged("SelectedAction");
            }
        }

        protected abstract void CreateVersions();
        
        // indicates whether the selected action is install or uninstall.
        bool _selectedActionIsInstall;

        public bool SelectedActionIsInstall
        {
            get
            {
                return _selectedActionIsInstall;
            }
            set
            {
                if (_selectedActionIsInstall != value)
                {
                    _selectedActionIsInstall = value;
                    OnPropertyChanged("SelectedActionIsInstall");
                }
            }
        }

        protected List<VersionForDisplay> _versions;

        public List<VersionForDisplay> Versions
        {
            get
            {
                return _versions;
            }
        }

        private VersionForDisplay _selectedVersion;

        public VersionForDisplay SelectedVersion
        {
            get
            {
                return _selectedVersion;
            }
            set
            {
                if (_selectedVersion != value)
                {
                    _selectedVersion = value;

                    UiPackageMetadata packageMetadata;
                    if (_metadataDict != null &&
                        _metadataDict.TryGetValue(_selectedVersion.Version, out packageMetadata))
                    {
                        PackageMetadata = packageMetadata;
                    }
                    else
                    {
                        PackageMetadata = null;
                    }
                    OnSelectedVersionChanged();
                    OnPropertyChanged("SelectedVersion");
                }
            }
        }

        public async Task LoadPackageMetadaAsync()
        {
            var metadata = await _searchResultPackage.GetPackageMetadataAsync();
            var dict = new Dictionary<NuGetVersion, UiPackageMetadata>();
            foreach (var item in metadata)
            {
                var packageMetadata = CreateDetailedPackage(item);
                dict[packageMetadata.Version] = packageMetadata;                
            }

            _metadataDict = dict;

            UiPackageMetadata p;
            if (SelectedVersion != null &&
                _metadataDict.TryGetValue(SelectedVersion.Version, out p))
            {
                PackageMetadata = p;
            }
        }

        public static UiPackageMetadata CreateDetailedPackage(JObject metadata)
        {            
            var detailedPackage = new UiPackageMetadata();
            detailedPackage.Version = NuGetVersion.Parse(metadata.Value<string>(Properties.Version));
            string publishedStr = metadata.Value<string>(Properties.Published);
            if (!String.IsNullOrEmpty(publishedStr))
            {
                detailedPackage.Published = DateTime.Parse(publishedStr);
            }

            detailedPackage.Summary = metadata.Value<string>(Properties.Summary);
            detailedPackage.Description = metadata.Value<string>(Properties.Description);
            detailedPackage.Authors = metadata.Value<string>(Properties.Authors);
            detailedPackage.Owners = metadata.Value<string>(Properties.Owners);
            detailedPackage.IconUrl = GetUri(metadata, Properties.IconUrl);
            detailedPackage.LicenseUrl = GetUri(metadata, Properties.LicenseUrl);
            detailedPackage.ProjectUrl = GetUri(metadata, Properties.ProjectUrl);
            detailedPackage.Tags = String.Join(" ", (metadata.Value<JArray>(Properties.Tags) ?? Enumerable.Empty<JToken>()).Select(t => t.ToString()));
            detailedPackage.DownloadCount = metadata.Value<int>(Properties.DownloadCount);
            detailedPackage.DependencySets = (metadata.Value<JArray>(Properties.DependencyGroups) ?? Enumerable.Empty<JToken>()).Select(obj => LoadDependencySet((JObject)obj));

            detailedPackage.HasDependencies = detailedPackage.DependencySets.Any(
                set => set.Dependencies != null && set.Dependencies.Count > 0);

            return detailedPackage;
        }

        private static Uri GetUri(JObject json, string property)
        {
            if (json[property] == null)
            {
                return null;
            }
            string str = json[property].ToString();
            if (String.IsNullOrEmpty(str))
            {
                return null;
            }
            return new Uri(str);
        }

        private static UiPackageDependencySet LoadDependencySet(JObject set)
        {
            var fxName = set.Value<string>(Properties.TargetFramework);
            return new UiPackageDependencySet(
                String.IsNullOrEmpty(fxName) ? null : FrameworkNameHelper.ParsePossiblyShortenedFrameworkName(fxName),
                (set.Value<JArray>(Properties.Dependencies) ?? Enumerable.Empty<JToken>()).Select(obj => LoadDependency((JObject)obj)));
        }

        private static UiPackageDependency LoadDependency(JObject dep)
        {
            var ver = dep.Value<string>(Properties.Range);
            return new UiPackageDependency(
                dep.Value<string>(Properties.PackageId),
                String.IsNullOrEmpty(ver) ? null : VersionRange.Parse(ver));
        }

        protected abstract void OnSelectedVersionChanged();

        public bool IsSolution
        {
            get
            {
                return _target.IsSolution;
            }
        }

        private Options _options;

        public Options Options
        {
            get
            {
                return _options;
            }
            set
            {
                _options = value;
                OnPropertyChanged("Options");
            }
        }
    }
}
