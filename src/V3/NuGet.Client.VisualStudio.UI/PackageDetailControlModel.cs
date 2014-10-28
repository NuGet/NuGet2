using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using NuGet.Versioning;
using Resx = NuGet.Client.VisualStudio.UI.Resources;

namespace NuGet.Client.VisualStudio.UI
{
    // The DataContext of the PackageDetail control is this class
    // It has two mode: Project, or Solution
    public class PackageDetailControlModel : INotifyPropertyChanged
    {
        private UiDetailedPackage _package;
        protected Dictionary<NuGetVersion, UiDetailedPackage> _allPackages;

        // used for data binding
        protected List<VersionForDisplay> _versions;
        private FileConflictActionItem[] _fileConflicActions;
        private DependencyBehaviorItem[] _dependencyBehaviors;

        public PackageDetailControlModel(
            UiSearchResultPackage searchResultPackage,
            NuGetVersion installedVersion)
        {
            _allPackages = new Dictionary<NuGetVersion, UiDetailedPackage>();
            foreach (var p in searchResultPackage.AllVersions)
            {
                _allPackages[p.Version] = p;
            }

            if (_allPackages.ContainsKey(searchResultPackage.Version))
            {
                _package = _allPackages[searchResultPackage.Version];
            }
            else
            {
                _package = _allPackages.Values.OrderByDescending(p => p.Version).FirstOrDefault();
            }
            CreateVersions(installedVersion);
            CreateFileConflictActions();
            CreateDependencyBehaviors();
        }

        private void CreateDependencyBehaviors()
        {
            _dependencyBehaviors = new[] 
            {
                new DependencyBehaviorItem(Resx.Resources.DependencyBehavior_IgnoreDependencies, DependencyBehavior.Ignore),
                new DependencyBehaviorItem(Resx.Resources.DependencyBehavior_Lowest, DependencyBehavior.Lowest),
                new DependencyBehaviorItem(Resx.Resources.DependencyBehavior_HighestPatch, DependencyBehavior.HighestPatch),
                new DependencyBehaviorItem(Resx.Resources.DependencyBehavior_HighestMinor, DependencyBehavior.HighestMinor),
                new DependencyBehaviorItem(Resx.Resources.DependencyBehavior_Highest, DependencyBehavior.Highest),
            };
            SelectedDependencyBehavior = _dependencyBehaviors[1];
        }

        private void CreateFileConflictActions()
        {
            _fileConflicActions = new []
            {
                new FileConflictActionItem(Resx.Resources.FileConflictAction_Prompt, FileConflictAction.PromptUser),
                new FileConflictActionItem(Resx.Resources.FileConflictAction_IgnoreAll, FileConflictAction.IgnoreAll),
                new FileConflictActionItem(Resx.Resources.FileConflictAction_OverwriteAll, FileConflictAction.OverwriteAll)
            };

            SelectedFileConflictAction = _fileConflicActions[0];
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
                _versions.Add(new VersionForDisplay(installedVersion, Resx.Resources.Version_Installed));
            }

            var allVersions = _allPackages.Keys.OrderByDescending(v => v);
            var latestStableVersion = allVersions.FirstOrDefault(v => !v.IsPrerelease);
            if (latestStableVersion != null)
            {
                _versions.Add(new VersionForDisplay(latestStableVersion, Resx.Resources.Version_LatestStable));
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

            if (_allPackages.ContainsKey(version))
            {
                Package = _allPackages[version];
            }
        }

        public IEnumerable<FileConflictActionItem> FileConflictActions
        {
            get
            {
                return _fileConflicActions;
            }
        }

        public FileConflictActionItem SelectedFileConflictAction
        {
            get;
            set;
        }

        public IEnumerable<DependencyBehaviorItem> DependencyBehaviors
        {
            get
            {
                return _dependencyBehaviors;
            }
        }

        public DependencyBehaviorItem SelectedDependencyBehavior
        {
            get;
            set;
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