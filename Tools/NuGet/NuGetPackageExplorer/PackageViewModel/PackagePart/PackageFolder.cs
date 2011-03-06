using System;
using System.Collections.Generic;

namespace PackageExplorerViewModel {
    public class PackageFolder : PackagePart {

        public SortedSet<PackagePart> Children { get; private set; }

        public PackageFolder(string name, string path, PackageFolder parent)
            : this(name, path, parent, new SortedSet<PackagePart>()) {

        }

        private PackageFolder(string name, string path, PackageFolder parent, SortedSet<PackagePart> children) : base(name, path, parent) {
            if (children == null) {
                throw new ArgumentNullException("children");
            }

            if (path == null) {
                throw new ArgumentNullException("path");
            }

            this.Children = children;
        }
    }
}