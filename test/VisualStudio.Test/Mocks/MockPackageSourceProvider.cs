using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio.Test
{
    public class MockPackageSourceProvider : IPackageSourceProvider
    {
        private List<PackageSource> _sources = new List<PackageSource>();

        public IEnumerable<PackageSource> LoadPackageSources()
        {
            return _sources;
        }

        public void SavePackageSources(IEnumerable<PackageSource> sources)
        {
            _sources = sources.ToList();

            if (PackageSourcesSaved != null)
            {
                PackageSourcesSaved(this, EventArgs.Empty);
            }
        }

        public void DisablePackageSource(PackageSource source)
        {
            var sourceInUse = _sources.Find(p => p.Equals(source));
            if (sourceInUse != null)
            {
                sourceInUse.IsEnabled = false;
            }
        }

        public bool IsPackageSourceEnabled(PackageSource source)
        {
            var sourceInUse = _sources.Find(p => p.Equals(source));
            return sourceInUse != null && sourceInUse.IsEnabled;
        }


        public event System.EventHandler PackageSourcesSaved;
    }
}