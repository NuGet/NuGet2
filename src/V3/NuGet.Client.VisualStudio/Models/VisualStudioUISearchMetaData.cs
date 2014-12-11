using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client;

namespace NuGet.Client.VisualStudio.Models
{
    /// <summary>
    /// Model for Search results displayed by Visual Studio Package Manager dialog UI.
    /// </summary>
    public class VisualStudioUISearchMetaData : SearchResult
    {
        //public string Id { get; set; }
        //public NuGetVersion Version { get; set; }
        //public string Summary { get; set; }
        //public Uri IconUrl { get; set; }
        //public IEnumerable<NuGetVersion> Versions { get; set; }
        public VisualStudioUIPackageMetadata latestPackageMetadata { get; set; }

    }
}

