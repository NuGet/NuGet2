using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace NuGet.Client.VisualStudio.UI
{
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

        public NuGetVersion Version { get; set; }

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

        public IEnumerable<NuGetVersion> Versions { get; set; }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }

        private SourceRepository _source;

        public UiSearchResultPackage(SourceRepository source)
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
            var metadata = await Task.Run(() => _source.GetPackageMetadataById(Id));
            return metadata;
        }
    }
}