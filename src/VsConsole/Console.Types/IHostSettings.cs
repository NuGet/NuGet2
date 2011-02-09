using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NuGetConsole {

    public interface IHostSettings : INotifyPropertyChanged {

        string ActivePackageSource { get; set; }

        ObservableCollection<string> PackageSources { get; }

        string DefaultProject { get; set; }

        ObservableCollection<string> AvailableProjects { get; }
    }
}