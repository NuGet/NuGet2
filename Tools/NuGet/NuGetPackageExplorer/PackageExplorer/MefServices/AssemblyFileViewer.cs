using PackageExplorerViewModel.Types;

namespace PackageExplorer {
    [PackageContentViewerMetadata(0, ".dll", ".exe")]
    internal class AssemblyFileViewer : IPackageContentViewer {
        public object GetView(System.IO.Stream stream) {
            return "This is a .NET assembly.";
        }
    }
}
