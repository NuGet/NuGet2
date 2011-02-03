using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PackageExplorerViewModel {
    public interface IShowFileContentHandler {

        void ShowFile(string name, string content);
    }
}
