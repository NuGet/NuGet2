using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.PackageManagerUI.Providers
{
    internal class PackageListItem : IVsExtension, INotifyPropertyChanged
    {
        private BitmapSource _previewImage;
        private FileVersionInfo _fileVersionInfo;
        private string _moreInfoUrl;
        private NuPack.Package _packageIdentity;

        private static ConcurrentDictionary<string, BitmapSource> s_previewImageDefault = new ConcurrentDictionary<string, BitmapSource>();

        /// <summary>
        /// The reference item is used within the extension manager dialog that we're using for Add Reference
        /// It will "house" the actual reference item that we'll use for the act of adding references
        /// </summary>
        public PackageListItem(NuPack.Package referenceRecord, bool isSelected, BitmapSource previewImage, float priority, BitmapSource thumbnail)
        {
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
        public NuPack.Package ExtensionRecord
        {
            get { return _packageIdentity; }
        }
                
        /// <summary>
        /// Name of this reference item
        /// </summary>
        public string Name
        {
            get
            {
                return _packageIdentity.Id;
            }
        }

        /// <summary>
        /// Description of this reference item
        /// </summary>
        public string Description
        {
            get
            {
                return _packageIdentity.Description;
            }
        }

        /// <summary>
        /// Version of the underlying reference
        /// </summary>
        public string Version
        {
            get
            {
                return _packageIdentity.Version.ToString();
            }
        }

        /// <summary>
        /// Is this reference selected
        /// </summary>
        public bool IsSelected
        {
            get;
            set;
        }

        /// <summary>
        /// The image to be used in the details pane for this reference
        /// </summary>
        public BitmapSource PreviewImage
        {
            get
            {
                if (_previewImage == null)
                {
                    return _previewImage = GetPreviewImage();
                }

                return _previewImage;
            }
            private set
            {
                _previewImage = value;
            }
        }

        /// <summary>
        /// Get the preview image for this reference
        /// </summary>
        private BitmapSource GetPreviewImage()
        {
            return null;
        }

        public float Priority
        {
            get;
            private set;
        }

        public BitmapSource ThumbnailImage
        {
            get;
            private set;
        }

#pragma warning disable 0067
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 0067

        public BitmapSource MediumThumbnailImage
        {
            get;
            private set;
        }

        public BitmapSource SmallThumbnailImage
        {
            get;
            private set;
        }

        public string NonNullName
        {
            get
            {
                return this.Name ?? string.Empty;
            }
        }

        public string Id
        {
            get { return _packageIdentity.Id; }
        }
    }
}
