using System.Collections.ObjectModel;
using NuGet;

namespace PackageExplorerViewModel.Types {
    public interface IMruManager {
        ObservableCollection<MruItem> Files { get; }
        void NotifyFileAdded(IPackageMetadata package, string filePath, PackageType packageType);
        void Clear();
        void OnApplicationExit();
    }
}