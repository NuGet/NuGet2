using System;

namespace PackageExplorerViewModel {
    public abstract class PackagePart {

        public string Name { get; private set; }

        protected PackagePart(string name) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }

            this.Name = name;
        }
    }
}
