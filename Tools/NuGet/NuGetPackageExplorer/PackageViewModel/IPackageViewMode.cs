using System.Collections.Generic;
using System.ComponentModel;
using NuGet;

namespace PackageExplorerViewModel {

    public interface IPackageViewModel : INotifyPropertyChanged {

        string PackageSource { get; set; }

        void ShowFile(string name, string content);

        bool OpenSaveFileDialog(string defaultName, out string selectedFileName);

        IEnumerable<IPackageFile> GetFiles();

        IPackageMetadata PackageMetadata { get; }

        bool HasEdit { get; }
          
        bool IsInEditMode { get; }

        void BegingEdit();

        void CancelEdit();

        void CommitEdit();

        void OnSaved();
    }
}