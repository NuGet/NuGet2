using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using NuGet.Client.Resources;

namespace NuGet.Client.VisualStudio.UI
{ 

    public class UiSearchResultPackage2 : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public VisualStudioUISearchMetaData searchMetaData;

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

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }

        private SourceRepository _source;

        public UiSearchResultPackage2(SourceRepository source)
        {
            _source = source;
        }

        public async Task<IEnumerable<JObject>> GetPackageMetadataAsync()
        {
            // We need to use Task.Run() here. Otherwise it might lead to deadlock.
            // The reason is that NuGet.Data.Client uses a spin lock on the URI.
            // If there are multiple calls to the same URI, then one could be spin waiting
            // the lock, on the UI thread, while another is waiting to be scheduled back to
            // the UI thread after an sync call. Using Task.Run(), the tasks will be scheduled
            // on thread pool, so they won't block each other.
            var metadata = await Task.Run(() => _source.GetPackageMetadataById(searchMetaData.Id));
            return metadata;
        }
    }
}