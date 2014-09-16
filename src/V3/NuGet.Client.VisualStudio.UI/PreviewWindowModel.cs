using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.UI
{
    public class PreviewWindowModel
    {
        public IEnumerable<PackageIdentity> Deleted
        {
            get;
            private set;
        }

        public IEnumerable<PackageIdentity> Added
        {
            get;
            private set;
        }

        public IEnumerable<PackageIdentity> Unchanged
        {
            get;
            private set;
        }

        public PreviewWindowModel(
            IEnumerable<PackageIdentity> added,
            IEnumerable<PackageIdentity> deleted,
            IEnumerable<PackageIdentity> unchanged)
        {
            Added = added;
            Deleted = deleted;
            Unchanged = unchanged;
        }
    }
}
