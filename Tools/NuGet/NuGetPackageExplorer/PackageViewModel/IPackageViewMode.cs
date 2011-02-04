
using NuGet;
using System.Collections.Generic;

namespace PackageExplorerViewModel {

    public interface IPackageViewModel {

        string PackageSource { get; }

        void ShowFile(string name, string content);

        bool OpenSaveFileDialog(string defaultName, out string selectedFileName);

        IEnumerable<IPackageFile> GetFiles();

        void SetEditMode();

        void CancelEditMode();
    }
}
