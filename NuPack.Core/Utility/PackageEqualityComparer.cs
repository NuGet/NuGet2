using System;
using System.Collections.Generic;

namespace NuGet {    
    internal class PackageEqualityComparer : IEqualityComparer<IPackage> {
        internal static PackageEqualityComparer IdAndVersionComparer = new PackageEqualityComparer((x, y) => x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase) && x.Version.Equals(y.Version),
                                                                                                             x => x.Id.GetHashCode() ^ x.Version.GetHashCode());

        internal static PackageEqualityComparer IdComparer = new PackageEqualityComparer((x, y) => x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase), x => x.Id.GetHashCode());

        private Func<IPackage, IPackage, bool> _equals;
        private Func<IPackage, int> _getHashCode;

        public PackageEqualityComparer(Func<IPackage, IPackage, bool> equals, Func<IPackage, int> getHashCode) {
            _equals = equals;
            _getHashCode = getHashCode;
        }

        public bool Equals(IPackage x, IPackage y) {
            return _equals(x, y);
        }

        public int GetHashCode(IPackage obj) {
            return _getHashCode(obj);
        }
    }
}
