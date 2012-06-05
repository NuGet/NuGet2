using System.Collections.Generic;
using System.Linq;
using NuGet.VisualStudio;

namespace NuGet.TestUI
{
    class MockPackageSourceProvider : IVsPackageSourceProvider
    {
        private List<PackageSource> _packageSources = new List<PackageSource>();

        public PackageSource ActivePackageSource
        {
            get;
            set;
        }

        public IEnumerable<PackageSource> LoadPackageSources()
        {
            return _packageSources;
        }

        public void AddPackageSource(PackageSource source)
        {
            _packageSources.Add(source);
        }

        public bool RemovePackageSource(PackageSource source)
        {
            return _packageSources.Remove(source);
        }

        public void SavePackageSources(IEnumerable<PackageSource> sources)
        {
            _packageSources = sources.ToList();
        }

        public void DisablePackageSource(PackageSource source)
        {
            var sourceInUse = _packageSources.Find(p => p.Equals(source));
            if (sourceInUse != null)
            {
                sourceInUse.IsEnabled = false;
            }
        }

        public bool IsPackageSourceEnabled(PackageSource source)
        {
            var sourceInUse = _packageSources.Find(p => p.Equals(source));
            return sourceInUse != null && sourceInUse.IsEnabled;
        }
    }
}
