
namespace PackageExplorerViewModel.Types {
    public interface IBindingGroup {
        void BeginEdit();
        void CancelEdit();
        bool CommitEdit();
    }
}
