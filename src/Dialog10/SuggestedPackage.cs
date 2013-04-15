using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.VisualStudio;

namespace NuGet.Dialog
{
    public class SuggestedPackage : IVsExtension, INotifyPropertyChanged
    {
        private readonly IPackageProvider _packageProvider;
        private bool _isSelected;

        public SuggestedPackage(IPackageProvider packageProvider)
        {
            if (packageProvider == null)
            {
                throw new ArgumentNullException("packageProvider");
            }

            _packageProvider = packageProvider;
        }

        public string Id
        {
            get { return _packageProvider.Name; }
        }

        public string Name
        {
            get { return _packageProvider.Name; }
        }

        public string Publisher
        {
            get { return _packageProvider.Publisher; }
        }

        public string Description
        {
            get { return _packageProvider.Description; }
        }

        public Uri IconUrl
        {
            get
            {
                return _packageProvider.IconUrl;
            }
        }

        public Uri PublisherUrl
        {
            get
            {
                return _packageProvider.PublisherUrl;
            }
        }


        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public float Priority
        {
            get { return 0; }
        }

        public BitmapSource PreviewImage
        {
            get { return null; }
        }

        public BitmapSource SmallThumbnailImage
        {
            get { return null; }
        }

        public BitmapSource MediumThumbnailImage
        {
            get { return null; }
        }

        public void Execute()
        {
            _packageProvider.Invoke();
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}