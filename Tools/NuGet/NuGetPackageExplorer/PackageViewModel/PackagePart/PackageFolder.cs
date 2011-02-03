using System;
using System.Collections.Generic;

namespace PackageExplorerViewModel {
    public class PackageFolder : PackagePart {

        public IList<PackagePart> Children { get; private set; }

        public PackageFolder(string name) : this(name, new List<PackagePart>()) {

        }

        public PackageFolder(string name, IList<PackagePart> children) : base(name) {
            if (children == null) {
                throw new ArgumentNullException("children");
            }

            this.Children = children;
        }
    }
}