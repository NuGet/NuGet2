using System;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.Providers {
    internal class OnlinePackagesTree : OnlinePackagesTreeBase {

        private string _category;

        public OnlinePackagesTree(
            OnlinePackagesProvider provider, 
            string category, 
            IVsExtensionsTreeNode parent) : 
            base(parent, provider) {

            if (category == null) {
                throw new ArgumentNullException("category");
            }
            _category = category;
        }

        public override string Name {
            get {
                return _category;
            }
        }

        protected override IQueryable<IPackage> PreviewQuery(IQueryable<IPackage> query) {
            return query;
        }

        protected override void FillNodes(System.Collections.Generic.IList<IVsExtensionsTreeNode> nodes) {
        }
    }
}
