using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Client.Resources
{
    public abstract class SearchResource
    {
        public abstract string Url { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public abstract Task<IEnumerable<VisualStudioUISearchResult>> GetSearchResultsForVisualStudioUI(
            string searchTerm,           
            SearchFilter filters,
            int skip,
            int take,
            CancellationToken cancellationToken);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public abstract Task<IEnumerable<CommandLineSearchResult>> GetSearchResultsForCommandLine(
            string searchTerm,
            bool includePrerelease,
            CancellationToken cancellationToken);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public abstract Task<IEnumerable<PowershellSearchResult>> GetSearchResultsForPowershellConsole(
            string searchTerm,
            SearchFilter filters,
            int skip,
            int take,
            CancellationToken cancellationToken);
    }
}
