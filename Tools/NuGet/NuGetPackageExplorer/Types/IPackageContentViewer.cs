using System.IO;

namespace NuGetPackageExplorer.Types {
    public interface IPackageContentViewer {
        object GetView(Stream stream);
    }
}