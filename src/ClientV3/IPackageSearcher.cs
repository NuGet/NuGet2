using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.VisualStudio.Client
{
    /// <summary>
    /// Represents the top-level interface to a NuGet Repository
    /// </summary>
    public interface IPackageSearcher
    {
        Task<IEnumerable<JToken>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken ct);
    }
}
