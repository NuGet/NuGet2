namespace NuPack.Test.Mocks {
    using System.Collections.Generic;
    using System.Linq;

    public class MockPackageRepository : PackageRepositoryBase {
        public MockPackageRepository() {
            Packages = new Dictionary<string, List<IPackage>>();
        }

        internal Dictionary<string, List<IPackage>> Packages {
            get;
            set;
        }

        public override void AddPackage(IPackage package) {
            AddPackage(package.Id, package);
        }

        public override IQueryable<IPackage> GetPackages() {
            return Packages.Values.SelectMany(p => p).AsQueryable();
        }        

        public override void RemovePackage(IPackage package) {
            List<IPackage> packages;
            if (Packages.TryGetValue(package.Id, out packages)) {
                packages.Remove(package);
            }

            if (packages.Count == 0) {
                Packages.Remove(package.Id);
            }
        }

        private void AddPackage(string id, IPackage package) {
            List<IPackage> packages;
            if (!Packages.TryGetValue(id, out packages)) {
                packages = new List<IPackage>();
                Packages.Add(id, packages);
            }
            packages.Add(package);
        }
    }
}
