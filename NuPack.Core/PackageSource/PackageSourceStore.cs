using System;
using System.Collections.Generic;
using System.Linq;

namespace NuPack {
    public static class PackageSourceStore {

        private static PackageSourceProvider _defaultProvider = new DefaultPackageSourceProvider();

        public static PackageSourceProvider DefaultProvider {
            get {
                return _defaultProvider;
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("value");
                }
                _defaultProvider = value;
            }
        }

        public static PackageSource ActivePackageSource {
            get {
                return DefaultProvider.ActivePackageSource;
            }
            set {
                DefaultProvider.ActivePackageSource = value;
            }
        }

        public static void AddPackageSource(PackageSource source) {
            DefaultProvider.AddPackageSource(source);
        }

        public static bool RemovePackageSource(PackageSource source) {
            return DefaultProvider.RemovePackageSource(source);
        }

        public static IEnumerable<PackageSource> GetPackageSources() {
            return DefaultProvider.GetPackageSources();
        }

        #region DefaultPackageSourceProvider

        private class DefaultPackageSourceProvider : PackageSourceProvider
        {
            public override PackageSource ActivePackageSource {
                get { return null; }
                set {}
            }

            public override IEnumerable<PackageSource> GetPackageSources() {
                return Enumerable.Empty<PackageSource>();
            }

            public override void AddPackageSource(PackageSource source) {
                throw new NotSupportedException("This operation is not supported by the default PackageSourceProvider.");
            }

            public override bool RemovePackageSource(PackageSource source) {
                return false;
            }
        }

        #endregion
    }
}