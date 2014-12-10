using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Client.VisualStudio.Models;
using NuGet.Client;

namespace NuGet.Client.VisualStudio.Repository
{
    public interface VsSearchResource
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
         Task<IEnumerable<VisualStudioUISearchMetaData>> GetSearchResultsForVisualStudioUI(
            string searchTerm,
            SearchFilter filters,
            int skip,
            int take,
            CancellationToken cancellationToken);
    }
}
