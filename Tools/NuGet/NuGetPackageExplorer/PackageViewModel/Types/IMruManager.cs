using System.Collections.ObjectModel;

namespace PackageExplorerViewModel.Types {
    public interface IMruManager {
        ObservableCollection<MruItem> Files { get; }
        void NotifyFileAdded(string filePath, string packageName, PackageType packageType);
        void Clear();
        void OnApplicationExit();
    }
}