using System;

namespace PackageExplorerViewModel {
    public abstract class PackagePart {

        public string Name { get; private set; }
        public string Path { get; private set; }

        protected PackagePart(string name, string path) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }

            if (path == null) {
                throw new ArgumentNullException("path");
            }

            this.Name = name;
            this.Path = path;
        }
    }
}
