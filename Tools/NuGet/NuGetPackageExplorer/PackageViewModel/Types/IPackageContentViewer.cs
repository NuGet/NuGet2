using System.IO;

namespace PackageExplorerViewModel.Types {
    public interface IPackageContentViewer {
        object GetView(Stream stream);
    }
}