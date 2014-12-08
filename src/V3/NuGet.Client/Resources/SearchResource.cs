using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Client.Resources
{
    /// <summary>
    /// Exposes Search functionality required by various clients.
    /// </summary>
    public abstract class SearchResource
    {
       
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public abstract Task<IEnumerable<VisualStudioUISearchMetaData>> GetSearchResultsForVisualStudioUI(
            string searchTerm,           
            SearchFilter filters,
            int skip,
            int take,
            CancellationToken cancellationToken);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public abstract Task<IEnumerable<CommandLineSearchMetadata>> GetSearchResultsForCommandLine(
            string searchTerm,
            bool includePrerelease,
            CancellationToken cancellationToken);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public abstract Task<IEnumerable<PowershellSearchMetadata>> GetSearchResultsForPowershellConsole(
            string searchTerm,
            SearchFilter filters,
            int skip,
            int take,
            CancellationToken cancellationToken);
    }
}
