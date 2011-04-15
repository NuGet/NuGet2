using System;
using System.Windows.Data;
using PackageExplorerViewModel.Types;

namespace PackageExplorer {
    public class BindingGroupWrapper : IBindingGroup {
        private readonly BindingGroup _bindingGroup;

        public BindingGroupWrapper(BindingGroup bindingGroup) {
            if (bindingGroup == null) {
                throw new ArgumentNullException("bindingGroup");
            }
            _bindingGroup = bindingGroup;
        }

        public void BeginEdit() {
            _bindingGroup.BeginEdit();
        }

        public void CancelEdit() {
            _bindingGroup.CancelEdit();
        }

        public bool CommitEdit() {
            return _bindingGroup.CommitEdit();
        }
    }
}
