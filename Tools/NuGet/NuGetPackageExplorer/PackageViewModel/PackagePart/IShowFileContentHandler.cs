using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PackageExplorerViewModel {
    public interface IPackageViewModel {

        void ShowFile(string name, string content);

        bool OpenSaveFileDialog(string defaultName, out string selectedFileName);
    }
}
