using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet
{
    internal class NullPackageRepository : IPackageRepository
    {
        internal static readonly NullPackageRepository Instance = new NullPackageRepository();
        private static readonly TraceSource Tracer = new TraceSource(typeof(NullPackageRepository).FullName);
        private NullPackageRepository() { }

        public string Source
        {
            get { return String.Empty; }
        }

        public PackageSaveModes PackageSaveMode
        {
            get
            {
                return PackageSaveModes.None;
            }
            set
            {
                // No-op!
                Tracer.TraceEvent(TraceEventType.Warning, 0, "Attempted to set PackageSaveMode on the null repository");
            }
        }

        public bool SupportsPrereleasePackages
        {
            get { return true; }
        }

        public IQueryable<IPackage> GetPackages()
        {
            return Enumerable.Empty<IPackage>().AsQueryable();
        }

        public void AddPackage(IPackage package)
        {
            // No-op!
            Tracer.TraceEvent(TraceEventType.Warning, 0, "Attempted to add a package to the null repository");
        }

        public void RemovePackage(IPackage package)
        {
            // No-op!
            Tracer.TraceEvent(TraceEventType.Warning, 1, "Attempted to remove a package to the null repository");
        }
    }
}
