using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.Providers {
    internal class OnlinePackagesItem : IVsExtension, INotifyPropertyChanged {
        private OnlinePackagesProvider _provider;
        private BitmapSource _previewImage;
        private NuPack.IPackage _packageIdentity;
        
        /// <summary>
        /// The reference item is used within the Add NuPack dialog that we're using for Add Reference
        /// It will "house" the actual reference item that we'll use for the act of adding references
        /// </summary>
        public OnlinePackagesItem(OnlinePackagesProvider provider, NuPack.IPackage referenceRecord, bool isSelected, BitmapSource previewImage, float priority, BitmapSource thumbnail) {
            _provider = provider;
            _packageIdentity = referenceRecord;

            PreviewImage = previewImage;
            Priority = priority;
            ThumbnailImage = thumbnail;
            MediumThumbnailImage = thumbnail;
            SmallThumbnailImage = thumbnail;
            IsSelected = isSelected;
        }

        /// <summary>
        /// The embedded reference record that will be used to interface with the dialog list
        /// </summary>
        public NuPack.IPackage ExtensionRecord {
            get { return _packageIdentity; }
        }

        /// <summary>
        /// Name of this reference item
        /// </summary>
        public string Name {
            get {
                return _packageIdentity.Id;
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

        /// <summary>
        /// The image to be used in the details pane for this reference
        /// </summary>
        public BitmapSource PreviewImage {
            get {
                return _previewImage;
            }
            private set {
                _previewImage = value;
            }
        }

        public float Priority {
            get;
            private set;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "We will need this property soon.")]
        public BitmapSource ThumbnailImage {
            get;
            private set;
        }

#pragma warning disable 0067
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 0067

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

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This property is data-bound in XAML")]
        public string Dependencies {
            get {
                StringBuilder dependencies = new StringBuilder();
                foreach (PackageDependency dependency in _packageIdentity.Dependencies) {
                    dependencies.Append("\r\n\t");
                    dependencies.Append(dependency);
                }
                return dependencies.Length > 0 ? "Dependencies:" + dependencies.ToString() : String.Empty;
            }
        }

        public bool IsInstalled {
            get {
                return _provider.IsInstalled(_packageIdentity.Id, _packageIdentity.Version);
            }
        }

        internal void UpdateInstallStatus() {
            OnNotifyPropertyChanged("IsInstalled");
        }

        public bool IsUpdated {
            get {
                return !_provider.CanBeUpdated(_packageIdentity);
            }
        }

        internal void UpdateUpdateStatus() {
            OnNotifyPropertyChanged("IsUpdated");
        }
    }
}