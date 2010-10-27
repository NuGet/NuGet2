using System.Collections.Generic;
using System.Linq;
using NuGet.VisualStudio;

namespace NuGet.TestUI {
    class MockPackageSourceProvider : IPackageSourceProvider {
        private IList<PackageSource> _packageSources = new List<PackageSource>();

        public PackageSource ActivePackageSource {
            get;
            set;
        }

        public IEnumerable<PackageSource> GetPackageSources() {
            return _packageSources;
        }

        public void AddPackageSource(PackageSource source) {
            _packageSources.Add(source);
        }

        public bool RemovePackageSource(PackageSource source) {
            return _packageSources.Remove(source);
        }

        public void SetPackageSources(IEnumerable<PackageSource> sources) {
            _packageSources = sources.ToList();
        }
    }
}
