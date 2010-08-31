namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    internal class PackagePlan {        
        public PackagePlan(IEnumerable<Package> packagesToInstall, IEnumerable<Package> packagesToUninstall) {
            PackagesToInstall = packagesToInstall;
            PackagesToUninstall = packagesToUninstall;
        }

        public IEnumerable<Package> PackagesToInstall {
            get;
            private set;
        }

        public IEnumerable<Package> PackagesToUninstall {
            get;
            private set;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            if (PackagesToUninstall.Any()) {
                sb.Append(String.Join(" => ", PackagesToUninstall.Select(p => "-" + p)));
            }

            if (PackagesToInstall.Any()) {
                if (sb.Length > 0) {
                    sb.Append(" => ");
                }
                sb.Append(String.Join(" => ", PackagesToInstall.Select(p => "+" + p)));
            }
            return sb.ToString();
        }
    }
}
