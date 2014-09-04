using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client
{
    public interface IPackageSearcher
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        Task<IEnumerable<JToken>> Search(
            string searchTerm,
            SearchFilter filters,
            int skip,
            int take,
            CancellationToken cancellationToken);
    }
}
