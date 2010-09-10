using System.Collections.Generic;
using System.Linq;

namespace NuPack {
    public abstract class PackageSourceProvider {

        private PackageSource _activeSource;

        protected PackageSourceProvider() {
        }

        public abstract IEnumerable<PackageSource> GetPackageSources();

        public abstract void AddPackageSource(PackageSource source);

        public abstract bool RemovePackageSource(PackageSource source);

        public virtual PackageSource ActivePackageSource {
            get {
                return _activeSource;
            }
            set {
                if (_activeSource != value) {
                    _activeSource = value;

                    // if the new source is not in the store, add it to the store
                    if (_activeSource != null && !GetPackageSources().Contains(_activeSource)) {
                        AddPackageSource(_activeSource);
                    }
                }
            }
        }
    }
}