namespace NuPack {
    using System;
    using System.Collections.Generic;

    internal class PackageComparer : IEqualityComparer<Package> {
        internal static PackageComparer IdAndVersionComparer = new PackageComparer((x, y) => x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase) && x.Version.Equals(y.Version),
                                                                      x => x.Id.GetHashCode() ^ x.Version.GetHashCode());

        internal static PackageComparer IdComparer = new PackageComparer((x, y) => x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase), x => x.Id.GetHashCode());

        private Func<Package, Package, bool> _equals;
        private Func<Package, int> _getHashCode;

        public PackageComparer(Func<Package, Package, bool> equals, Func<Package, int> getHashCode) {
            _equals = equals;
            _getHashCode = getHashCode;
        }

        public bool Equals(Package x, Package y) {
            return _equals(x, y);
        }

        public int GetHashCode(Package obj) {
            return _getHashCode(obj);
        }
    }
}