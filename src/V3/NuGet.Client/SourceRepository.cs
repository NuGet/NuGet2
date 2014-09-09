using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client
{
    /// <summary>
    /// Represents a place where packages can be retrieved
    /// </summary>
    public abstract class SourceRepository
    {
        public abstract PackageSource Source { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public abstract Task<IEnumerable<JObject>> Search(
            string searchTerm,
            // REVIEW: Do we use parameters instead of this object? What about adding filter criteria?
            SearchFilter filters,
            int skip,
            int take,
            CancellationToken cancellationToken);
    }
}
