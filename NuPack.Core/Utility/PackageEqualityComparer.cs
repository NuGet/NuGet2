using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NuGet {
    public class PackageEqualityComparer : IEqualityComparer<IPackage> {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "This type isn't mutable")]
        public static readonly PackageEqualityComparer IdAndVersion = new PackageEqualityComparer((x, y) => x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase) &&
                                                                                                                    x.Version.Equals(y.Version),
                                                                                                   x => x.Id.GetHashCode() ^ x.Version.GetHashCode());

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "This type isn't mutable")]
        public static readonly PackageEqualityComparer Id = new PackageEqualityComparer((x, y) => x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase),
                                                                                         x => x.Id.GetHashCode());

        private readonly Func<IPackage, IPackage, bool> _equals;
        private readonly Func<IPackage, int> _getHashCode;

        private PackageEqualityComparer(Func<IPackage, IPackage, bool> equals, Func<IPackage, int> getHashCode) {
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