using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio.Test {
    public class MockPackageSourceProvider : IPackageSourceProvider {
        private List<PackageSource> _sources = new List<PackageSource>();

        public IEnumerable<PackageSource> LoadPackageSources() {
            return _sources;
        }

        public void SavePackageSources(IEnumerable<PackageSource> sources) {
            _sources = sources.ToList();
        }
    }
}