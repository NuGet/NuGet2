using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media.Imaging;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers {

    internal class PackageItem : IVsExtension, INotifyPropertyChanged {
        private readonly PackagesProviderBase _provider;
        private readonly IPackage _packageIdentity;
        private readonly bool _isUpdateItem;
        private bool _isSelected;
        private readonly ObservableCollection<Project> _referenceProjectNames;

        public PackageItem(PackagesProviderBase provider, IPackage package, bool isUpdateItem = false) :
            this(provider, package, new Project[0], isUpdateItem) {
        }

        public PackageItem(PackagesProviderBase provider, IPackage package, IEnumerable<Project> referenceProjectNames, bool isUpdateItem = false) {
            _provider = provider;
            _packageIdentity = package;
            _isUpdateItem = isUpdateItem;
            _referenceProjectNames = new ObservableCollection<Project>(referenceProjectNames);
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public IPackage PackageIdentity {
            get { return _packageIdentity; }
        }

        public string Id {
            get { return _packageIdentity.Id; }
        }

        public string Name {
            get {
                return String.IsNullOrEmpty(_packageIdentity.Title) ? _packageIdentity.Id : _packageIdentity.Title;
            }
        }

        public string Version {
            get {
                return _packageIdentity.Version.ToString();
            }
        }

        public bool IsUpdateItem {
            get {
                return _isUpdateItem;
            }
        }

        public string Description {
            get {
                if (_isUpdateItem && !String.IsNullOrEmpty(_packageIdentity.ReleaseNotes)) {
                    return _packageIdentity.ReleaseNotes;
                }

                return _packageIdentity.Description;
            }
        }

        public string Summary {
            get {
                return String.IsNullOrEmpty(_packageIdentity.Summary) ? _packageIdentity.Description : _packageIdentity.Summary;
            }
        }

        public IEnumerable<PackageDependency> Dependencies {
            get {
                return _packageIdentity.Dependencies;
            }
        }

        public ICollection<Project> ReferenceProjects {
            get {
                return _referenceProjectNames;
            }
        }

        public string CommandName {
            get;
            set;
        }

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This property is data-bound in XAML.")]
        public IEnumerable<string> Authors {
            get {
                return _packageIdentity.Authors;
            }
        }

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This property is data-bound in XAML.")]
        public bool RequireLicenseAcceptance {
            get {
                return _packageIdentity.RequireLicenseAcceptance;
            }
        }

        public Uri LicenseUrl {
            get {
                return _packageIdentity.LicenseUrl;
            }
        }

        public bool IsSelected {
            get {
                return _isSelected;
            }
            set {
                _isSelected = value;
                OnNotifyPropertyChanged("IsSelected");
            }
        }

        public bool IsEnabled {
            get {
                return _provider.CanExecute(this);
            }
        }

        internal void UpdateEnabledStatus() {
            OnNotifyPropertyChanged("IsEnabled");
        }

        private void OnNotifyPropertyChanged(string propertyName) {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        // Not used but required by the interface IVsExtension.
        public float Priority {
            get { return 0; }
        }

        // Not used but required by the interface IVsExtension.
        public BitmapSource MediumThumbnailImage {
            get { return null; }
        }

        // Not used but required by the interface IVsExtension.
        public BitmapSource SmallThumbnailImage {
            get { return null; }
        }

        // Not used but required by the interface IVsExtension.
        public BitmapSource PreviewImage {
            get { return null; }
        }
    }
}