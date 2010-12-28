using System;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers {

    /// <summary>
    /// This tree node simply shows no packages.
    /// </summary>
    internal class EmptyTreeNode : PackagesTreeNodeBase {

         private readonly string _category;

        public EmptyTreeNode(PackagesProviderBase provider, string category, IVsExtensionsTreeNode parent) : 
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

        public override IQueryable<IPackage> GetPackages() {
            return Enumerable.Empty<IPackage>().AsQueryable();
        }
    }
}
