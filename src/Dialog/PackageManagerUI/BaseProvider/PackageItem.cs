using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers {

    internal class PackageItem : IVsExtension, INotifyPropertyChanged {

        private PackagesProviderBase _provider;
        private IPackage _packageIdentity;
        
        /// <summary>
        /// The reference item is used within the Add Package dialog.
        /// It will "house" the actual reference item that we'll use for the act of adding references
        /// </summary>
        public PackageItem(PackagesProviderBase provider, IPackage package, BitmapSource thumbnail) {

            _provider = provider;
            _packageIdentity = package;

            Priority = 0;
            ThumbnailImage = thumbnail;
            MediumThumbnailImage = thumbnail;
            SmallThumbnailImage = thumbnail;
        }

        /// <summary>
        /// The embedded reference record that will be used to interface with the dialog list
        /// </summary>
        public IPackage PackageIdentity {
            get { return _packageIdentity; }
        }

        /// <summary>
        /// Name of this reference item
        /// </summary>
        public string Name {
            get {
                return String.IsNullOrEmpty(_packageIdentity.Title) ? _packageIdentity.Id : _packageIdentity.Title;
            }
        }

        /// <summary>
        /// Description of this reference item
        /// </summary>
        public string Description {
            get {
                return _packageIdentity.Description;
            }
        }

        public string Summary {
            get {
                return String.IsNullOrEmpty(_packageIdentity.Summary) ? _packageIdentity.Description : _packageIdentity.Summary;
            }
        }

        /// <summary>
        /// Version of the underlying reference
        /// </summary>
        public string Version {
            get {
                return _packageIdentity.Version.ToString();
            }
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

        /// <summary>
        /// Is this reference selected
        /// </summary>
        /// 
        bool isSelected;
        public bool IsSelected {
            get {
                return isSelected;
            }
            set {
                isSelected = value;
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

        /// <summary>
        /// The image to be used in the details pane for this reference
        /// </summary>
        public BitmapSource PreviewImage {
            get;
            set;
        }

        public float Priority {
            get;
            private set;
        }

        public string CommandName {
            get;
            set;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "We will need this property soon.")]
        public BitmapSource ThumbnailImage {
            get;
            private set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnNotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public BitmapSource MediumThumbnailImage {
            get;
            private set;
        }

        public BitmapSource SmallThumbnailImage {
            get;
            private set;
        }

        public string Id {
            get { return _packageIdentity.Id; }
        }

        public IEnumerable<PackageDependency> Dependencies {
            get {
                return _packageIdentity.Dependencies;
            }
        }
    }
}
