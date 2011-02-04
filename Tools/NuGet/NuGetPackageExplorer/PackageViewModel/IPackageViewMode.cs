using System.Collections.Generic;
using System.ComponentModel;
using NuGet;

namespace PackageExplorerViewModel {

    public interface IPackageViewModel : INotifyPropertyChanged {

        string PackageSource { get; }

        void ShowFile(string name, string content);

        bool OpenSaveFileDialog(string defaultName, out string selectedFileName);

        IEnumerable<IPackageFile> GetFiles();

        bool HasEdit { get; }
          
        bool IsInEditMode { get; }

        void BegingEdit();

        void CancelEdit();

        void CommitEdit();
    }
}