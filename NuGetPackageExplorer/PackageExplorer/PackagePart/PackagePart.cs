
namespace PackageExplorer {
    public abstract class PackagePart {

        public string Name { get; private set; }

        protected PackagePart(string name) {
            this.Name = name;
        }
    }
}
