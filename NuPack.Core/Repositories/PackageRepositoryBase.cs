namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class PackageRepositoryBase : IPackageRepository {
        private Dictionary<Tuple<string, Version>, IPackage> _packageCache = new Dictionary<Tuple<string, Version>, IPackage>(PackageIdAndVersionComparer.Default);

        public abstract IQueryable<IPackage> GetPackages();

        public virtual IPackage FindPackage(string packageId, Version version) {
            var key = Tuple.Create(packageId, version);

            IPackage package;
            if (!_packageCache.TryGetValue(key, out package)) {
                package = GetPackages().FirstOrDefault(p => p.Id == packageId && p.Version == version);

                if (package != null) {
                    _packageCache[key] = package;
                }
            }

            return package;
        }

        public virtual void AddPackage(IPackage package) {
            var key = Tuple.Create(package.Id, package.Version);

            _packageCache[key] = package;
        }

        public virtual void RemovePackage(IPackage package) {
            var key = Tuple.Create(package.Id, package.Version);

            _packageCache.Remove(key);
        }

        internal class PackageIdAndVersionComparer : IEqualityComparer<Tuple<string, Version>> {
            internal static PackageIdAndVersionComparer Default = new PackageIdAndVersionComparer();

            public bool Equals(Tuple<string, Version> x, Tuple<string, Version> y) {
                return x.Item1.Equals(y.Item1, StringComparison.OrdinalIgnoreCase) && 
                       x.Item2 == y.Item2;
            }

            public int GetHashCode(Tuple<string, Version> obj) {
                return obj.GetHashCode();
            }
        }
    }
}
