using System;
using System.Collections.Generic;

namespace PackageExplorerViewModel {

    internal class PackagePartComparer : IComparer<PackagePart> {

        public int Compare(PackagePart x, PackagePart y) {
            if (x == y) {
                return 0;
            }

            if (x == null) {
                return -1;
            }

            if (y == null) {
                return 1;
            }

            // folder goes before file
            if (x is PackageFolder && y is PackageFile) {
                return -1;
            }

            if (x is PackageFile && y is PackageFolder) {
                return 1;
            }

            return String.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
