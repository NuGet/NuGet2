namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class PackagePlan {
        public PackagePlan(IEnumerable<IPackage> packagesToInstall, IEnumerable<IPackage> packagesToUninstall) {
            PackagesToInstall = packagesToInstall;
            PackagesToUninstall = packagesToUninstall;
        }

        public IEnumerable<IPackage> PackagesToInstall {
            get;
            private set;
        }

        public IEnumerable<IPackage> PackagesToUninstall {
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
