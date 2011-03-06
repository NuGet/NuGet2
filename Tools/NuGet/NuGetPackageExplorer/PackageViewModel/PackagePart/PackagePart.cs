using System;

namespace PackageExplorerViewModel {
    public abstract class PackagePart : IComparable<PackagePart> {

        public string Name { get; private set; }
        public string Path { get; private set; }
        public PackageFolder Parent { get; private set; }

        protected PackagePart(string name, string path, PackageFolder parent) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }

            if (path == null) {
                throw new ArgumentNullException("path");
            }

            this.Name = name;
            this.Path = path;
            this.Parent = parent;
        }

        public int CompareTo(PackagePart other) {
            if (this == other) {
                return 0;
            }

            if (other == null) {
                return 1;
            }

            // folder goes before file
            if (this is PackageFolder && other is PackageFile) {
                return -1;
            }

            if (this is PackageFile && other is PackageFolder) {
                return 1;
            }

            return String.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
