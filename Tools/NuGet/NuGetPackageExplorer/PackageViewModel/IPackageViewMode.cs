using NuGet;
using System.Collections.Generic;
using System.ComponentModel;

namespace PackageExplorerViewModel {

    public interface IPackageViewModel : INotifyPropertyChanged {

        string PackageSource { get; }

        void ShowFile(string name, string content);

        bool OpenSaveFileDialog(string defaultName, out string selectedFileName);

        IEnumerable<IPackageFile> GetFiles();
          
        bool IsInEditMode { get; }

        void StartEditMode();

        void CancelEditMode();

        void ApplyPackageMetadataChanges();

        IPackageMetadata PackageMetadata { get; }
    }
}