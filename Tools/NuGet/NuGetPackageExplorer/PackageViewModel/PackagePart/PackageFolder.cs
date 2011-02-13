using System;
using System.Collections.Generic;

namespace PackageExplorerViewModel {
    public class PackageFolder : PackagePart {

        public List<PackagePart> Children { get; private set; }
        
        public PackageFolder(string name, string path) : this(name, path, new List<PackagePart>()) {

        }

        private PackageFolder(string name, string path, List<PackagePart> children) : base(name, path) {
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