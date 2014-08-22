using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio.Client
{
    public interface INuGetRepository
    {
        /// <summary>
        /// Creates a searcher that will return results implementing all of the specified types
        /// </summary>
        /// <param name="requiredResultTypes">A list of URIs defining types of data required, see <see cref="Uris"/></param>
        /// <returns>A searcher that can be used to find results implementing the specified types</returns>
        IPackageSearcher CreateSearcher(params Uri[] requiredResultTypes);
    }
}
