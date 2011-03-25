using System.Collections.ObjectModel;

namespace PackageExplorerViewModel.Types
{
    public interface IMruPackageSourceManager
    {
        string ActivePackageSource { get; set; }
        ObservableCollection<string> PackageSources { get; }
        void NotifyPackageSourceAdded(string newSource);
        void OnApplicationExit();
    }
}
