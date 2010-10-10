using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.Providers {
    internal class OnlinePackagesItem : IVsExtension, INotifyPropertyChanged {
        private OnlinePackagesProvider _provider;
        private BitmapSource _previewImage;
        private NuPack.IPackage _packageIdentity;

        private static ConcurrentDictionary<string, BitmapSource> s_previewImageDefault = new ConcurrentDictionary<string, BitmapSource>();

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

        public IEnumerable<string> Authors {
            get {
                return _packageIdentity.Authors;
            }
        }

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
                if (_previewImage == null) {
                    return _previewImage = GetPreviewImage();
                }

                return _previewImage;
            }
            private set {
                _previewImage = value;
            }
        }

        /// <summary>
        /// Get the preview image for this reference
        /// </summary>
        private BitmapSource GetPreviewImage() {
            return null;
        }

        public float Priority {
            get;
            private set;
        }

        public BitmapSource ThumbnailImage {
            get;
            private set;
        }

#pragma warning disable 0067
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 0067

        private void OnNotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                try {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                catch {
                }
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

        public string NonNullName {
            get {
                return this.Name ?? string.Empty;
            }
        }

        public string Id {
            get { return _packageIdentity.Id; }
        }

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

        public bool IsUpdated {
            get {
                return !_provider.CanBeUpdated(_packageIdentity);
            }
        }
    }
}