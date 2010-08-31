namespace NuPack.Test.Mocks {
    using System.Collections.Generic;
    using System.Linq;

    public class MockPackageRepository : PackageRepositoryBase {
        public MockPackageRepository() {
            Packages = new Dictionary<string, List<Package>>();
        }

        internal Dictionary<string, List<Package>> Packages {
            get;
            set;
        }

        public override void AddPackage(Package package) {
            AddPackage(package.Id, package);
        }

        public override IQueryable<Package> GetPackages() {
            return Packages.Values.SelectMany(p => p).AsQueryable();
        }        

        public override void RemovePackage(Package package) {
            List<Package> packages;
            if (Packages.TryGetValue(package.Id, out packages)) {
                packages.Remove(package);
            }

            if (packages.Count == 0) {
                Packages.Remove(package.Id);
            }
        }

        private void AddPackage(string id, Package package) {
            List<Package> packages;
            if (!Packages.TryGetValue(id, out packages)) {
                packages = new List<Package>();
                Packages.Add(id, packages);
            }
            packages.Add(package);
        }
    }
}
