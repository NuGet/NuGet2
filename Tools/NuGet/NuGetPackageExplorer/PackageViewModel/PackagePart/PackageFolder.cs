using System;
using System.Collections.Generic;

namespace PackageExplorerViewModel {
    public class PackageFolder : PackagePart {

        public IList<PackagePart> Children { get; private set; }
        
        public PackageFolder(string name, string path) : this(name, path, new List<PackagePart>()) {

        }

        public PackageFolder(string name, string path, IList<PackagePart> children) : base(name, path) {
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