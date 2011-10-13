using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NuGet
{
    public class PackageComparer : IComparer<IPackage>
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "This type isn't mutable")]
        public static readonly PackageComparer Version = new PackageComparer((x, y) => x.Version.CompareTo(y.Version));

        private readonly Func<IPackage, IPackage, int> _compareTo;
        private PackageComparer(Func<IPackage, IPackage, int> compareTo)
        {
            _compareTo = compareTo;
        }

        public int Compare(IPackage x, IPackage y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }
            return _compareTo(x, y);
        }
    }
}
