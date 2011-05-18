
namespace PackageExplorerViewModel.Types {
    public interface IPackageContentViewerMetadata {
        int Order { get; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        string[] SupportedExtensions { get; }
    }
}
